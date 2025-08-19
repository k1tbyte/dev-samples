import { Kafka } from "kafkajs";
import { KafkaListener } from "./src/KafkaListener.ts";
import {EmailSender} from "./src/senders/email/emailSender.ts";
import {GoogleSmtpProvider} from "./src/senders/email/providers/GoogleSmtpProvider.ts";
import {SmsSender} from "./src/senders/sms/smsSender.ts";
import {WhatsAppProvider} from "./src/senders/sms/providers/WhatsAppProvider.ts";

// Kafka config
const kafka = new Kafka({
    clientId: "mailer-service",
    brokers: [process.env.KAFKA_BROKER!],
});

const kafkaListener = new KafkaListener(kafka, "notifications", [
    new EmailSender(new GoogleSmtpProvider()),
    new SmsSender(new WhatsAppProvider())
]);

kafkaListener.start().catch((err) => {
    console.error("KafkaListener error:", err);
    process.exit(1);
});

process.on("SIGINT", async () => {
    console.log("Stopping notification service...");
    await kafkaListener.stop();
    process.exit(0);
});
