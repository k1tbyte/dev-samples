export enum ETemplateId {
    WELCOME = 'WELCOME',
    ACCOUNT_VERIFICATION = 'ACCOUNT_VERIFICATION',
  //  PASSWORD_RESET = 'PASSWORD_RESET'
}

export type WelcomeTemplateData = {
    username: string;
}

export type AccountVerificationTemplateData = {
    link: string;
    username: string;
}

export type PasswordResetTemplateData = {
    resetLink: string;
    username: string;
    expirationTime?: string;
}