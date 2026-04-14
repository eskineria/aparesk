export type AuditLogItem = {
    id: number
    userId: string | null
    serviceName: string
    methodName: string
    parameters: string
    executionTime: string
    executionDuration: number
    clientIpAddress: string | null
    browserInfo: string | null
    exception: string | null
}

export type AuditLogFilterOptions = {
    services: string[]
    methods: string[]
}

export type AuditAlert = {
    key: string
    severity: string
    message: string
    metricValue: number
}

export type AuditAlertsSummary = {
    generatedAtUtc: string
    alerts: AuditAlert[]
}

export type AuditIntegritySummary = {
    featureEnabled: boolean
    integrityTableExists: boolean
    totalAuditLogCount: number
    hardenedLogCount: number
    missingHardeningCount: number
    brokenChainCount: number
    lastHardenedAuditLogId: number | null
    lastVerifiedAtUtc: string
    brokenSampleAuditLogIds: number[]
    missingSampleAuditLogIds: number[]
}

export type AuditFieldDiff = {
    field: string
    before: string | null
    after: string | null
    changed: boolean
}

export type AuditDiffResult = {
    logId: number
    comparedLogId: number | null
    source: 'none' | 'disabled' | 'payload' | 'previous_log' | 'snapshot'
    hasComparableData: boolean
    changes: AuditFieldDiff[]
}

export type GetAuditLogsParams = {
    id?: number
    pageNumber?: number
    pageSize?: number
    searchTerm?: string
    serviceName?: string
    methodName?: string
    userId?: string
    onlyErrors?: boolean
    fromUtc?: string
    toUtc?: string
}

export type PagedAuditLogResult = {
    items: AuditLogItem[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages: number
}
