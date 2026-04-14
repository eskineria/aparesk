import api from '@/api/axios';
import type {
    TermsDto,
    ComplianceResponse,
    AcceptTermsDto,
    UserTermsAcceptanceDto,
    CreateTermsDto,
    UpdateTermsDto,
} from '@/types/compliance';

export const ComplianceService = {
    getAllTerms: async (type?: string) => {
        const url = type ? `/compliance/terms?type=${type}` : '/compliance/terms';
        const response = await api.get<ComplianceResponse<TermsDto[]>>(url);
        return response.data;
    },
    getActiveTerms: async (type: string) => {
        const response = await api.get<ComplianceResponse<TermsDto>>(`/compliance/terms/active/${type}`);
        return response.data;
    },
    getActiveTermsOptional: async (type: string) => {
        const response = await api.get<ComplianceResponse<TermsDto | null>>(`/compliance/terms/active-optional/${type}`);
        return response.data;
    },
    getTermsById: async (id: string) => {
        const response = await api.get<ComplianceResponse<TermsDto>>(`/compliance/terms/${id}`);
        return response.data;
    },
    createTerms: async (dto: CreateTermsDto) => {
        const response = await api.post<ComplianceResponse<TermsDto>>('/compliance/terms', dto);
        return response.data;
    },
    updateTerms: async (id: string, dto: UpdateTermsDto) => {
        const response = await api.put<ComplianceResponse<null>>(`/compliance/terms/${id}`, dto);
        return response.data;
    },
    activateTerms: async (id: string) => {
        const response = await api.post<ComplianceResponse<null>>(`/compliance/terms/${id}/activate`);
        return response.data;
    },
    deleteTerms: async (id: string) => {
        const response = await api.delete<ComplianceResponse<null>>(`/compliance/terms/${id}`);
        return response.data;
    },
    acceptTerms: async (dto: AcceptTermsDto) => {
        const response = await api.post<ComplianceResponse<null>>('/compliance/accept', dto);
        return response.data;
    },
    getUserAcceptances: async () => {
        const response = await api.get<ComplianceResponse<UserTermsAcceptanceDto[]>>('/compliance/my-acceptances');
        return response.data;
    },
    checkAcceptance: async (type: string) => {
        const response = await api.get<ComplianceResponse<boolean>>(`/compliance/check-acceptance/${type}`);
        return response.data;
    },
    getPendingRequiredTerms: async () => {
        const response = await api.get<ComplianceResponse<TermsDto[]>>('/compliance/pending-required');
        return response.data;
    }
};
