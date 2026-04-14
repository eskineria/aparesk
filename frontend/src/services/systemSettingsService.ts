import api from '@/api/axios'
import type {
    ApiResponse,
    AuthSystemSettings,
    BrandingAssetUploadResult,
    UpdateAuthSystemSettingsRequest,
} from '@/types/systemSettings'

export const SystemSettingsService = {
    getPublicAuthSettings: async () => {
        const response = await api.get<ApiResponse<AuthSystemSettings>>('/SystemSettings/public')
        return response.data
    },

    getAuthSettings: async () => {
        const response = await api.get<ApiResponse<AuthSystemSettings>>('/SystemSettings')
        return response.data
    },

    updateAuthSettings: async (data: UpdateAuthSystemSettingsRequest) => {
        const response = await api.put<ApiResponse<null>>('/SystemSettings', data)
        return response.data
    },

    uploadApplicationLogo: async (file: File) => {
        const formData = new FormData()
        formData.append('file', file)
        const response = await api.post<ApiResponse<BrandingAssetUploadResult>>('/SystemSettings/branding/logo', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        })
        return response.data
    },

    uploadApplicationFavicon: async (file: File) => {
        const formData = new FormData()
        formData.append('file', file)
        const response = await api.post<ApiResponse<BrandingAssetUploadResult>>('/SystemSettings/branding/favicon', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        })
        return response.data
    },
}

export default SystemSettingsService
