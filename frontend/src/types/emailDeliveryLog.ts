export type EmailDeliveryLogItem = {
    id: number
    channel: string
    recipient: string
    subject: string
    templateKey: string | null
    culture: string | null
    status: string | null
    providerName: string | null
    messageId: string | null
    errorMessage: string | null
    createdAt: string
}

export type GetEmailDeliveryLogsParams = {
    pageNumber?: number
    pageSize?: number
    searchTerm?: string
    templateKey?: string
    status?: string
    fromUtc?: string
    toUtc?: string
}

export type PagedEmailDeliveryLogResult = {
    items: EmailDeliveryLogItem[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages: number
}
