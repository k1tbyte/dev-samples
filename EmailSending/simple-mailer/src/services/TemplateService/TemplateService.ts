import fs from "fs";
import handlebars from "handlebars";
import path from "path";
import mjml2html from "mjml";
import {
    type AccountVerificationTemplateData,
    ETemplateId,
    type WelcomeTemplateData
} from "./types.ts";


interface TemplateDataMap {
    [ETemplateId.WELCOME]: WelcomeTemplateData;
    [ETemplateId.ACCOUNT_VERIFICATION]: AccountVerificationTemplateData;
   // [ETemplateId.PASSWORD_RESET]: PasswordResetTemplateData;
}

const templateRegistry: Record<ETemplateId, string> = {
    [ETemplateId.WELCOME]: "welcome.mjml",
    [ETemplateId.ACCOUNT_VERIFICATION]: "account-verification.mjml",
  //  [ETemplateId.PASSWORD_RESET]: "password-reset.mjml"
};

class TemplateService {
    private cache = new Map<ETemplateId, handlebars.TemplateDelegate>();
    private readonly basePath: string;

    constructor(basePath:string = "./templates") {
        this.basePath = basePath;
        this.validateTemplates();
    }

    /**
     * Validation of the existence of all template files during initialization
     */
    private validateTemplates(): void {
        Object.entries(templateRegistry).forEach(([templateId, templatePath]) => {
            const absolutePath = path.resolve(this.basePath, templatePath);
            if (!fs.existsSync(absolutePath)) {
                throw new Error(`Template file not found: ${templatePath} for template ${templateId}`);
            }
        });
    }

    /**
     * Obtaining a compiled template with caching
     */
    private getTemplate<T extends ETemplateId>(id: T): handlebars.TemplateDelegate<TemplateDataMap[T]> {
        if (this.cache.has(id)) {
            return this.cache.get(id)! as handlebars.TemplateDelegate<TemplateDataMap[T]>;
        }

        const templatePath = templateRegistry[id];
        if (!templatePath) {
            throw new Error(`Template "${id}" not registered`);
        }

        try {
            const absolutePath = path.resolve(this.basePath, templatePath);
            const src = fs.readFileSync(absolutePath, "utf-8");
            const compiled = handlebars.compile<TemplateDataMap[T]>(src);

            this.cache.set(id, compiled as handlebars.TemplateDelegate);
            return compiled;
        } catch (error) {
            throw new Error(`Failed to compile template "${id}": ${error}`);
        }
    }

    /**
     * Generating HTML markup from a template with typed data
     */
    public generateMarkup<T extends ETemplateId>(
        id: T,
        data: TemplateDataMap[T]
    ): string {
        const template = this.getTemplate(id);
        const mjml = template(data);
        const { html } = mjml2html(mjml, { validationLevel: "strict" });
        return html;
    }

    /**
     * Clear cache (useful for tests or hot reload)
     */
    public clearCache(): void {
        this.cache.clear();
    }

    /**
     * Precompile all templates (for warm-up)
     */
    public precompileAll(): void {
        Object.keys(templateRegistry).forEach(templateId => {
            this.getTemplate(templateId as ETemplateId);
        });
    }
}


export const templateService = new TemplateService();


export const generateMarkup = <T extends ETemplateId>(
    id: T,
    data: TemplateDataMap[T]
): string => {
    return templateService.generateMarkup(id, data);
};

export const precompileTemplates = (): void => {
    templateService.precompileAll();
};


export type TemplateData<T extends ETemplateId> = TemplateDataMap[T];