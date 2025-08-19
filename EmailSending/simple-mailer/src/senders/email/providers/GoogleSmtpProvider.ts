import type {IEmailProvider} from "../../types.ts";
import {createTransport, type Transporter} from "nodemailer";

export class GoogleSmtpProvider implements IEmailProvider {
    private transporter: Transporter = createTransport({
        host: process.env.SMTP_HOST,
        port: parseInt(process.env.SMTP_PORT!),
        secure: process.env.SMTP_SECURE === 'true',
        auth: {
            user: process.env.SMTP_USERNAME,
            pass: process.env.SMTP_PASSWORD,
        },
    });

    sendEmail(to: string, subject: string, html?: string, text?: string): Promise<void> {
        throw new Error("Method not implemented.");

/*        await this.transporter.sendMail({
            from: process.env.SMTP_FROM,
            to: message.to,
            subject: message.subject || 'No subject',
            html,
        });*/
    }
}