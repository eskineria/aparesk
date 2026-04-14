import api from '@/api/axios'
import type { GetEmailDeliveryLogsParams, PagedEmailDeliveryLogResult } from '@/types/emailDeliveryLog'

export const EmailDeliveryLogService = {
    getPaged: async (params?: GetEmailDeliveryLogsParams) => {
        const response = await api.get<PagedEmailDeliveryLogResult>('/EmailDeliveryLogs', { params })
        return response.data
    },
}

export default EmailDeliveryLogService
