import { z } from 'zod';


export const AccountVerificationSchema = z.object({
    to: z.email(),
    link: z.string(),
    username: z.string()
})

export type TypeAccountVerification = z.infer<typeof AccountVerificationSchema>;
export type TypePasswordReset = z.infer<typeof AccountVerificationSchema>;