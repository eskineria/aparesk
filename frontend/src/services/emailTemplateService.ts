import api from '@/api/axios'
import type {
    ApiResponse,
    AutoTranslateCultureRequest,
    AutoTranslateEmailTemplateRequest,
    AutoTranslateEmailTemplateResult,
    CreateEmailTemplateRequest,
    EmailTemplate,
    EmailTemplateCoverageItem,
    EmailTemplateRevision,
    EmailTemplateValidationResult,
    PublishEmailTemplateRequest,
    RollbackEmailTemplateRequest,
    SendEmailTemplateTestRequest,
    UpdateEmailTemplateRequest,
    ValidateEmailTemplateRequest,
} from '@/types/emailTemplate'

export const EmailTemplateService = {
    getAll: async (culture?: string) => {
        const response = await api.get<ApiResponse<EmailTemplate[]>>('/EmailTemplates', {
            params: culture ? { culture } : undefined,
        })
        return response.data
    },

    getById: async (id: number) => {
        const response = await api.get<ApiResponse<EmailTemplate>>(`/EmailTemplates/${id}`)
        return response.data
    },

    create: async (data: CreateEmailTemplateRequest) => {
        const response = await api.post<ApiResponse<EmailTemplate>>('/EmailTemplates', data)
        return response.data
    },

    update: async (id: number, data: UpdateEmailTemplateRequest) => {
        const response = await api.put<ApiResponse<EmailTemplate>>(`/EmailTemplates/${id}`, data)
        return response.data
    },

    publish: async (id: number, data: PublishEmailTemplateRequest = {}) => {
        const response = await api.post<ApiResponse<EmailTemplate>>(`/EmailTemplates/${id}/publish`, data)
        return response.data
    },

    rollback: async (id: number, data: RollbackEmailTemplateRequest) => {
        const response = await api.post<ApiResponse<EmailTemplate>>(`/EmailTemplates/${id}/rollback`, data)
        return response.data
    },

    getVersions: async (id: number) => {
        const response = await api.get<ApiResponse<EmailTemplateRevision[]>>(`/EmailTemplates/${id}/versions`)
        return response.data
    },

    getCoverage: async () => {
        const response = await api.get<ApiResponse<EmailTemplateCoverageItem[]>>('/EmailTemplates/coverage')
        return response.data
    },

    validate: async (data: ValidateEmailTemplateRequest) => {
        const response = await api.post<ApiResponse<EmailTemplateValidationResult>>('/EmailTemplates/validate', data)
        return response.data
    },

    sendTest: async (data: SendEmailTemplateTestRequest) => {
        const response = await api.post<ApiResponse<null>>('/EmailTemplates/send-test', data)
        return response.data
    },

    autoTranslate: async (data: AutoTranslateEmailTemplateRequest) => {
        const response = await api.post<ApiResponse<AutoTranslateEmailTemplateResult>>(
            '/EmailTemplates/auto-translate',
            data,
        )
        return response.data
    },

    autoTranslateCulture: async (data: AutoTranslateCultureRequest) => {
        const response = await api.post<ApiResponse<AutoTranslateEmailTemplateResult>>(
            '/EmailTemplates/auto-translate-culture',
            data,
        )
        return response.data
    },

    delete: async (id: number) => {
        const response = await api.delete<ApiResponse<null>>(`/EmailTemplates/${id}`)
        return response.data
    },
}

export default EmailTemplateService
