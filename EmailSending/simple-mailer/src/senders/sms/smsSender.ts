import {type INotificationSender, type ISmsProvider, NotificationType} from "../types.ts";

export class SmsSender implements INotificationSender {
    public readonly Type: NotificationType = NotificationType.SMS;
    private readonly provider: ISmsProvider;

    constructor(provider: ISmsProvider) {
        this.provider = provider;
    }

    sendNotification(reason: string, payload: any): Promise<void> {
        console.log("Sending SMS notification:", reason, payload);
        return new Promise(() => {

        })
    }

}