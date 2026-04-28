export type PagedResult<T> = {
    items: T[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages: number
}

export type PagedRequest = {
    pageNumber: number
    pageSize: number
    searchTerm?: string
    isActive?: boolean
    includeArchived?: boolean
}

export type SiteListItem = {
    id: string
    name: string
    city?: string | null
    district?: string | null
    isActive: boolean
    isArchived: boolean
    blockCount: number
    unitCount: number
    updatedAtUtc: string
}

export type SiteDetail = SiteListItem & {
    taxNumber?: string | null
    taxOffice?: string | null
    phone?: string | null
    email?: string | null
    addressLine?: string | null
    postalCode?: string | null
    createdAtUtc: string
    archivedAtUtc?: string | null
}

export type SitePayload = {
    name: string
    taxNumber?: string | null
    taxOffice?: string | null
    phone?: string | null
    email?: string | null
    addressLine?: string | null
    district?: string | null
    city?: string | null
    postalCode?: string | null
    isActive: boolean
}

export type BlockListItem = {
    id: string
    siteId: string
    siteName: string
    name: string
    floorCount?: number | null
    isActive: boolean
    isArchived: boolean
    unitCount: number
    updatedAtUtc: string
}

export type BlockDetail = BlockListItem & {
    description?: string | null
    createdAtUtc: string
    archivedAtUtc?: string | null
}

export type BlockPayload = {
    siteId: string
    name: string
    floorCount?: number | null
    unitsPerFloor?: number | null
    description?: string | null
    isActive: boolean
}

export const UnitType = {
    Apartment: 1,
    Shop: 2,
    Office: 3,
    Storage: 4,
    Parking: 5,
    CommonArea: 6,
    Other: 99,
} as const

export type UnitType = typeof UnitType[keyof typeof UnitType]

export type UnitListItem = {
    id: string
    siteId: string
    siteName: string
    siteBlockId?: string | null
    blockName?: string | null
    number: string
    doorNumber?: string | null
    type: UnitType
    floorNumber?: number | null
    grossAreaSquareMeters?: number | null
    netAreaSquareMeters?: number | null
    landShare?: number | null
    isActive: boolean
    isArchived: boolean
    updatedAtUtc: string
}

export type UnitDetail = UnitListItem & {
    notes?: string | null
    createdAtUtc: string
    archivedAtUtc?: string | null
}

export type UnitPayload = {
    siteId: string
    siteBlockId?: string | null
    number: string
    doorNumber?: string | null
    type: UnitType
    floorNumber?: number | null
    grossAreaSquareMeters?: number | null
    netAreaSquareMeters?: number | null
    landShare?: number | null
    notes?: string | null
    isActive: boolean
}

export const ResidentType = {
    Owner: 1,
    Tenant: 2,
    FamilyMember: 3,
    AuthorizedPerson: 4,
    Other: 99,
} as const

export type ResidentType = typeof ResidentType[keyof typeof ResidentType]

export type ResidentListItem = {
    id: string
    siteId: string
    siteName: string
    unitId?: string | null
    unitNumber?: string | null
    blockName?: string | null
    firstName: string
    lastName: string
    fullName: string
    type: ResidentType
    phone?: string | null
    email?: string | null
    moveInDate?: string | null
    moveOutDate?: string | null
    isActive: boolean
    isArchived: boolean
    updatedAtUtc: string
}

export type ResidentDetail = ResidentListItem & {
    identityNumber?: string | null
    occupation?: string | null
    kvkkConsentGiven: boolean
    communicationConsentGiven: boolean
    notes?: string | null
    createdAtUtc: string
    archivedAtUtc?: string | null
    ownerFirstName?: string | null
    ownerLastName?: string | null
    ownerPhone?: string | null
    householdMembers: HouseholdMember[]
}

export type HouseholdMember = {
    id?: string
    firstName: string
    lastName: string
    phone?: string | null
    identityNumber?: string | null
    relationship?: string | null
}

export type ResidentPayload = {
    siteId: string
    unitId?: string | null
    firstName: string
    lastName: string
    identityNumber?: string | null
    type: ResidentType
    phone?: string | null
    email?: string | null
    occupation?: string | null
    moveInDate?: string | null
    moveOutDate?: string | null
    kvkkConsentGiven: boolean
    communicationConsentGiven: boolean
    notes?: string | null
    isActive: boolean
    ownerFirstName?: string | null
    ownerLastName?: string | null
    ownerPhone?: string | null
    householdMembers: HouseholdMember[]
}

export const MeetingType = {
    Ordinary: 1,
    Extraordinary: 2,
} as const

export type MeetingType = typeof MeetingType[keyof typeof MeetingType]

export const BoardType = {
    ManagementBoard: 1,
    AuditBoard: 2,
} as const

export type BoardType = typeof BoardType[keyof typeof BoardType]

export const BoardMemberType = {
    Principal: 1,
    Substitute: 2,
} as const

export type BoardMemberType = typeof BoardMemberType[keyof typeof BoardMemberType]

export type GeneralAssemblyAgendaItem = {
    order: number
    description: string
}

export type GeneralAssemblyDecision = {
    id?: string
    decisionNumber: number
    description: string
}

export type GeneralAssemblyBoardMember = {
    id?: string
    residentId: string
    boardType: BoardType
    memberType: BoardMemberType
    title?: string | null
    // View fields
    residentName?: string
}

export type GeneralAssemblyListItem = {
    id: string
    siteId: string
    siteName: string
    meetingDate: string
    term: string
    type: MeetingType
    isCompleted: boolean
    updatedAtUtc: string
}

export type GeneralAssemblyDetail = GeneralAssemblyListItem & {
    agendaItems: GeneralAssemblyAgendaItem[]
    decisions: GeneralAssemblyDecision[]
    boardMembers: GeneralAssemblyBoardMember[]
}

export type GeneralAssemblyPayload = {
    siteId: string
    meetingDate: string
    term: string
    type: MeetingType
    isCompleted: boolean
    agendaItems: GeneralAssemblyAgendaItem[]
    decisions: GeneralAssemblyDecision[]
    boardMembers: GeneralAssemblyBoardMember[]
}
