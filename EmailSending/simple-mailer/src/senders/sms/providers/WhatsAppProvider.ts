import type {ISmsProvider} from "../../types.ts";

export class WhatsAppProvider implements ISmsProvider {
    sendSms(to: string, message: string): Promise<void> {
        throw new Error("Method not implemented.");
    }
}