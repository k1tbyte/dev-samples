import {type IEmailProvider, NotificationType, type SubjectList} from "../types.ts";
import {AccountVerificationSchema, type TypeAccountVerification, type TypePasswordReset} from "./types.ts";
import {BaseNotificationSender} from "../BaseNotificationSender.ts";
import {generateMarkup} from "../../services/TemplateService/TemplateService.ts";
import {ETemplateId} from "../../services/TemplateService/types.ts";


export class EmailSender extends BaseNotificationSender {
    private readonly provider: IEmailProvider;

    public readonly Type: NotificationType = NotificationType.EMAIL;
    protected readonly Subjects: SubjectList = {
        ACCOUNT_VERIFICATION: {
            handler: EmailSender.prototype.onAccountVerification,
            schema: AccountVerificationSchema
        },
        PASSWORD_RESET: {
            handler: EmailSender.prototype.onPasswordReset,
            schema: AccountVerificationSchema
        }
    }

    constructor(provider: IEmailProvider) {
        super();
        this.provider = provider;
    }

    private async onAccountVerification( { to, link, username }: TypeAccountVerification) {
        await this.provider.sendEmail(to,
            `Account Verification for ${username}`,
            generateMarkup(ETemplateId.ACCOUNT_VERIFICATION, {
                link,
                username
            })
        )
    }

    private async onPasswordReset({ to, link, username }: TypePasswordReset) {
        console.log("Sending password reset email to:", to, link, username);
    }
}