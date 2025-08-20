import {type INotificationSender, NotificationType, type SubjectList} from "./types.ts";

export abstract class BaseNotificationSender implements INotificationSender {
    public abstract readonly Type: NotificationType;
    protected abstract readonly Subjects: SubjectList;

    sendNotification(subject: string, payload: any) {
        const subjectEntry = this.Subjects[subject];
        if (!subjectEntry) {
            console.error(`[${this.Type}] Unknown subject: ${subject}`);
            return Promise.resolve();
        }
        const validatedPayload = subjectEntry.schema.safeParse(payload);
        if (!validatedPayload.success) {
            return Promise.resolve();
        }
        return subjectEntry.handler.call(this, validatedPayload.data);
    }
}