export const enum NotificationType {
    SMS = 'sms',
    EMAIL = 'email',
    PUSH = 'push',
    WEBHOOK = 'webhook',
    IN_APP = 'in_app'
}

export interface INotificationSender {
    Type: NotificationType;
    sendNotification(reason: string, payload: any): Promise<void>;
}

export interface ISmsProvider {
    sendSms(to: string, text: string): Promise<void>;
}

export interface IEmailProvider {
    sendEmail(to: string, subject: string, html?: string, text?: string): Promise<void>;
}

export type MessageType = {
    reason: string;
    payload: any;
}