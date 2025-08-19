import {type IEmailProvider, type INotificationSender, type ISmsProvider, NotificationType} from "../types.ts";

export class EmailSender implements INotificationSender {
    public readonly Type: NotificationType = NotificationType.EMAIL;
    private readonly provider: IEmailProvider;

    constructor(provider: IEmailProvider) {
        this.provider = provider;
    }

    sendNotification(reason: string, payload: any): Promise<void> {
        console.log("Sending email notification:", reason, payload);
        return new Promise(() => {

        })
    }
}