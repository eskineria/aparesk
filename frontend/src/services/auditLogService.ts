import api from '@/api/axios'
import type {
    AuditDiffResult,
    AuditLogFilterOptions,
    AuditIntegritySummary,
    GetAuditLogsParams,
    PagedAuditLogResult,
} from '@/types/auditLog'

export const AuditLogService = {
    getPaged: async (params?: GetAuditLogsParams) => {
        const response = await api.get<PagedAuditLogResult>('/AuditLogs', { params })
        return response.data
    },

    getFilterOptions: async () => {
        const response = await api.get<AuditLogFilterOptions>('/AuditLogs/filters')
        return response.data
    },


    getIntegrity: async () => {
        const response = await api.get<AuditIntegritySummary>('/AuditLogs/integrity')
        return response.data
    },

    getDiff: async (id: number) => {
        const response = await api.get<AuditDiffResult>(`/AuditLogs/${id}/diff`)
        return response.data
    },
}

export default AuditLogService
