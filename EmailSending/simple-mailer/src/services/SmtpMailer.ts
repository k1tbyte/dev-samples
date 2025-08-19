/*import type {IMessageSender, MessagePayload} from "../interfaces/IMessageSender";
import { createTransport, type Transporter } from "nodemailer";
import mjml2html from "mjml";
import fs from "fs";
import handlebars from "handlebars";


export class SmtpMailer implements IMessageSender {
    private transporter: Transporter;
    private mjmlTemplate: HandlebarsTemplateDelegate;

    constructor() {
        this.transporter = createTransport({
            host: process.env.SMTP_HOST,
            port: parseInt(process.env.SMTP_PORT!),
            secure: process.env.SMTP_SECURE === 'true',
            auth: {
                user: process.env.SMTP_USERNAME,
                pass: process.env.SMTP_PASSWORD,
            },
        });
        const mjmlTemplateSrc = fs.readFileSync("./src/templates/welcome.mjml", "utf-8");
        this.mjmlTemplate = handlebars.compile(mjmlTemplateSrc);
    }

    async sendMessage(message: MessagePayload): Promise<void> {
        if (message.type !== 'email') throw new Error('SmtpMailer поддерживает только email');
        const mjmlMarkup = this.mjmlTemplate(message.templateData || {});
        const { html, errors } = mjml2html(mjmlMarkup, { minify: true });
        if (errors && errors.length) {
            throw new Error('Ошибка генерации MJML: ' + JSON.stringify(errors));
        }

    }
}

*/
