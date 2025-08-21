import { z } from 'zod';


export const AccountVerificationSchema = z.object({
    email: z.email(),
    link: z.string(),
    username: z.string()
})

export type TypeAccountVerification = z.infer<typeof AccountVerificationSchema>;
export type TypePasswordReset = z.infer<typeof AccountVerificationSchema>;