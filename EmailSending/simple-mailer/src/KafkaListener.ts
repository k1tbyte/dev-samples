import { Kafka } from "kafkajs";
import type { Consumer } from "kafkajs";
import type {INotificationSender } from "./senders/types.ts";

export class KafkaListener {
    private consumer: Consumer;
    private readonly topic: string;
    private readonly senders: Record<string, INotificationSender>;

    constructor(kafka: Kafka, topic: string, senders: INotificationSender[]) {
        this.consumer = kafka.consumer({ groupId: "notification-service" });
        this.topic = topic;
        this.senders = senders.reduce((acc, sender) => {
            acc[sender.Type] = sender;
            return acc;
        }, {} as Record<string, INotificationSender>);
    }

    async start() {
        await this.consumer.connect();
        await this.consumer.subscribe({ topic: this.topic, fromBeginning: true });
        console.log(`Listening Kafka topic: ${this.topic}`);
        await this.consumer.run({
            eachMessage: async ({ message }) => {
                try {
                    if(!message.key || !message.value || !message.headers?.type) {
                        return;
                    }

                    const senderType = message.headers.type.toString();
                    const payload: any = JSON.parse(message.value?.toString());

                    const sender = this.senders[senderType];
                    if (!sender) {
                        console.error(`No sender found for type: ${senderType}`);
                        return;
                    }

                    await sender.sendNotification(message.key.toString(), payload)

                } catch (err) {
                    console.error("Error:", err);
                }
            },
        });
    }

    async stop() {
        await this.consumer.disconnect();
    }
}
