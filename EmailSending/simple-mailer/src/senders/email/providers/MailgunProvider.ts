import type {IEmailProvider} from "../../types.ts";

export class MailgunProvider implements IEmailProvider {
    sendEmail(to: string, subject: string, html?: string, text?: string): Promise<void> {
        throw new Error("Method not implemented.");
    }
}