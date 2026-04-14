export interface TermsDto {
    id: string;
    type: string;
    version: string;
    content: string;
    summary: string | null;
    effectiveDate: string;
    createdAt: string;
    isActive: boolean;
}

export interface CreateTermsDto {
    type: string;
    version: string;
    content: string;
    summary?: string | null;
    effectiveDate: string;
}

export interface UpdateTermsDto {
    content: string;
    summary?: string | null;
    isActive: boolean;
}

export interface UserTermsAcceptanceDto {
    id: string;
    userId: string;
    termsAndConditionsId: string;
    acceptedAt: string;
    ipAddress?: string;
    userAgent?: string;
    terms?: TermsDto;
}

export interface AcceptTermsDto {
    termsAndConditionsId: string;
    ipAddress?: string;
    userAgent?: string;
}

export interface ComplianceResponse<T> {
    success: boolean;
    message: string;
    data: T;
    errors: string[];
}
