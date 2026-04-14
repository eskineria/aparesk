import axios from '@/api/axios';

export interface LanguageResource {
    id: number;
    key: string;
    value: string;
    draftValue?: string | null;
    culture: string;
    resourceSet: string;
    workflowStatus?: string;
    ownerUserId?: string | null;
    lastPublishedAtUtc?: string | null;
    lastPublishedByUserId?: string | null;
    lastModifiedAtUtc?: string | null;
    lastModifiedByUserId?: string | null;
}

export interface LocalizationResponse {
    items: LanguageResource[];
    totalCount: number;
}

export interface LocalizationCapabilities {
    draftPublishEnabled: boolean;
    workflowEnabled: boolean;
    missingTranslationBannerEnabled: boolean;
}

export interface LocalizationMissingKeysResult {
    featureEnabled: boolean;
    requestedCulture: string;
    matchedCulture: string | null;
    fallbackCulture: string | null;
    missingKeys: string[];
}

const localizationService = {
    getList: async (search?: string, culture?: string, page: number = 1, pageSize: number = 20) => {
        const response = await axios.get<LocalizationResponse>('/localization', {
            params: { search, culture, page, pageSize }
        });
        return response.data;
    },

    getById: async (id: number) => {
        const response = await axios.get<LanguageResource>(`/localization/${id}`);
        return response.data;
    },

    getCapabilities: async () => {
        const response = await axios.get<LocalizationCapabilities>('/localization/capabilities');
        return response.data;
    },

    create: async (resource: Partial<LanguageResource>) => {
        const response = await axios.post<LanguageResource>('/localization', resource);
        return response.data;
    },

    update: async (id: number, resource: Partial<LanguageResource>) => {
        const response = await axios.put<LanguageResource>(`/localization/${id}`, resource);
        return response.data;
    },

    publish: async (id: number) => {
        const response = await axios.post<{ message?: string }>(`/localization/${id}/publish`);
        return response.data;
    },

    delete: async (id: number) => {
        await axios.delete(`/localization/${id}`);
    },

    deleteCulture: async (culture: string) => {
        await axios.delete(`/localization/cultures/${encodeURIComponent(culture)}`);
    },

    getCultures: async () => {
        const response = await axios.get<string[]>('/localization/cultures');
        return response.data;
    },

    clone: async (sourceCulture: string, targetCulture: string) => {
        const response = await axios.post('/localization/clone', { sourceCulture, targetCulture });
        return response.data;
    },

    getMissingKeys: async (culture?: string) => {
        const response = await axios.get<LocalizationMissingKeysResult>('/localization/missing-keys', {
            params: { culture }
        });
        return response.data;
    },
};

export default localizationService;
