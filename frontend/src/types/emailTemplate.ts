export type EmailTemplate = {
    id: number
    key: string
    culture: string
    name: string
    subject: string
    body: string
    requiredVariables: string[]
    isActive: boolean
    isDraft: boolean
    currentVersion: number
    publishedVersion: number | null
    publishedAt: string | null
    publishedByUserId: string | null
    autoTranslatedFromCulture: string | null
    autoTranslatedAt: string | null
    createdAt: string
    updatedAt: string
}

export type CreateEmailTemplateRequest = {
    key: string
    culture: string
    name: string
    subject: string
    body: string
    requiredVariables?: string[]
    isActive: boolean
    publishNow?: boolean
}

export type UpdateEmailTemplateRequest = {
    name: string
    subject: string
    body: string
    requiredVariables?: string[]
    isActive: boolean
    publishNow?: boolean
}

export type ApiResponse<T> = {
    success: boolean
    message: string
    data: T
}

export type EmailTemplateRevision = {
    id: number
    version: number
    name: string
    subject: string
    body: string
    requiredVariables: string[]
    isPublishedSnapshot: boolean
    changeSource: string
    changedByUserId: string | null
    createdAt: string
}

export type EmailTemplateCoverageItem = {
    key: string
    totalCultures: number
    missingCultures: number
    missingCultureCodes: string[]
    draftCultureCodes: string[]
}

export type PublishEmailTemplateRequest = {
    markActive?: boolean
}

export type RollbackEmailTemplateRequest = {
    version: number
    publishNow?: boolean
}

export type ValidateEmailTemplateRequest = {
    subject: string
    body: string
    requiredVariables?: string[]
}

export type EmailTemplateValidationResult = {
    isValid: boolean
    usedVariables: string[]
    normalizedRequiredVariables: string[]
    missingRequiredVariables: string[]
    extraVariables: string[]
    errors: string[]
}

export type SendEmailTemplateTestRequest = {
    templateId?: number
    key?: string
    culture?: string
    toEmail: string
    usePublishedVersion?: boolean
    variables?: Record<string, string>
}

export type AutoTranslateEmailTemplateRequest = {
    key: string
    sourceCulture: string
    targetCulture: string
    overwriteExisting?: boolean
}

export type AutoTranslateCultureRequest = {
    sourceCulture: string
    targetCulture: string
    overwriteExisting?: boolean
}

export type AutoTranslateEmailTemplateResult = {
    createdCount: number
    updatedCount: number
    skippedCount: number
}
