import api from '@/api/axios'
import type {
    BlockDetail,
    BlockListItem,
    BlockPayload,
    PagedRequest,
    PagedResult,
    SiteDetail,
    SiteListItem,
    SitePayload,
    UnitDetail,
    UnitListItem,
    UnitPayload,
    ResidentDetail,
    ResidentListItem,
    ResidentPayload,
} from '@/types/propertyManagement'

type BackendPagedResponse<T> = {
    data?: T[] | null
    count?: number | null
    index?: number | null
    size?: number | null
    pages?: number | null
}

type BackendDataResponse<T> = {
    data?: T | null
}

const normalizePagedResult = <T>(payload?: BackendPagedResponse<T> | null): PagedResult<T> => {
    const items = Array.isArray(payload?.data) ? payload!.data : []
    const totalCount = typeof payload?.count === 'number' ? payload.count : 0
    const pageNumber = typeof payload?.index === 'number' ? payload.index + 1 : 1
    const pageSize = typeof payload?.size === 'number' ? payload.size : items.length
    const totalPages = typeof payload?.pages === 'number' ? payload.pages : 0

    return { items, totalCount, pageNumber, pageSize, totalPages }
}

export const PropertyManagementService = {
    getSites: async (params: PagedRequest) => {
        const response = await api.get<BackendPagedResponse<SiteListItem>>('/Sites', { params })
        return normalizePagedResult(response.data)
    },
    getSite: async (id: string) => {
        const response = await api.get<BackendDataResponse<SiteDetail>>(`/Sites/${id}`)
        return response.data.data
    },
    createSite: async (data: SitePayload) => {
        const response = await api.post<BackendDataResponse<SiteDetail>>('/Sites', data)
        return response.data.data
    },
    updateSite: async (id: string, data: SitePayload) => {
        const response = await api.put<BackendDataResponse<SiteDetail>>(`/Sites/${id}`, data)
        return response.data.data
    },
    archiveSite: async (id: string) => {
        const response = await api.delete(`/Sites/${id}`)
        return response.data
    },

    getBlocks: async (params: PagedRequest & { siteId?: string }) => {
        const response = await api.get<BackendPagedResponse<BlockListItem>>('/Blocks', { params })
        return normalizePagedResult(response.data)
    },
    getBlock: async (id: string) => {
        const response = await api.get<BackendDataResponse<BlockDetail>>(`/Blocks/${id}`)
        return response.data.data
    },
    createBlock: async (data: BlockPayload) => {
        const response = await api.post<BackendDataResponse<BlockDetail>>('/Blocks', data)
        return response.data.data
    },
    updateBlock: async (id: string, data: Omit<BlockPayload, 'siteId' | 'unitsPerFloor'>) => {
        const response = await api.put<BackendDataResponse<BlockDetail>>(`/Blocks/${id}`, data)
        return response.data.data
    },
    archiveBlock: async (id: string) => {
        const response = await api.delete(`/Blocks/${id}`)
        return response.data
    },

    getUnits: async (params: PagedRequest & { siteId?: string; siteBlockId?: string }) => {
        const response = await api.get<BackendPagedResponse<UnitListItem>>('/Units', { params })
        return normalizePagedResult(response.data)
    },
    getUnit: async (id: string) => {
        const response = await api.get<BackendDataResponse<UnitDetail>>(`/Units/${id}`)
        return response.data.data
    },
    createUnit: async (data: UnitPayload) => {
        const response = await api.post<BackendDataResponse<UnitDetail>>('/Units', data)
        return response.data.data
    },
    updateUnit: async (id: string, data: Omit<UnitPayload, 'siteId'>) => {
        const response = await api.put<BackendDataResponse<UnitDetail>>(`/Units/${id}`, data)
        return response.data.data
    },
    archiveUnit: async (id: string) => {
        const response = await api.delete(`/Units/${id}`)
        return response.data
    },

    getResidents: async (params: PagedRequest & { siteId?: string; unitId?: string; type?: number }) => {
        const response = await api.get<BackendPagedResponse<ResidentListItem>>('/Residents', { params })
        return normalizePagedResult(response.data)
    },
    getResident: async (id: string) => {
        const response = await api.get<BackendDataResponse<ResidentDetail>>(`/Residents/${id}`)
        return response.data.data
    },
    createResident: async (data: ResidentPayload) => {
        const response = await api.post<BackendDataResponse<ResidentDetail>>('/Residents', data)
        return response.data.data
    },
    updateResident: async (id: string, data: Omit<ResidentPayload, 'siteId'>) => {
        const response = await api.put<BackendDataResponse<ResidentDetail>>(`/Residents/${id}`, data)
        return response.data.data
    },
    getGeneralAssemblies: async (params: PagedRequest & { siteId?: string }) => {
        // Mock implementation for now
        const items = JSON.parse(localStorage.getItem('mockGeneralAssemblies') || '[]')
        return { items, totalCount: items.length, pageNumber: 1, pageSize: 10, totalPages: 1 } as PagedResult<any>
    },
    getGeneralAssembly: async (id: string) => {
        const items = JSON.parse(localStorage.getItem('mockGeneralAssemblies') || '[]')
        return items.find((x: any) => x.id === id) || null
    },
    createGeneralAssembly: async (data: any) => {
        const items = JSON.parse(localStorage.getItem('mockGeneralAssemblies') || '[]')
        let siteName = 'Bilinmeyen Site'
        try {
            const response = await api.get(`/Sites/${data.siteId}`)
            siteName = response.data?.data?.name || siteName
        } catch (e) {
            console.error('Could not fetch site name for mock', e)
        }
        const newItem = { ...data, id: Math.random().toString(36).substring(7), meetingDate: data.meetingDate, siteName, updatedAtUtc: new Date().toISOString() }
        items.push(newItem)
        localStorage.setItem('mockGeneralAssemblies', JSON.stringify(items))
        return newItem
    },
    updateGeneralAssembly: async (id: string, data: any) => {
        const items = JSON.parse(localStorage.getItem('mockGeneralAssemblies') || '[]')
        const index = items.findIndex((x: any) => x.id === id)
        if (index !== -1) {
            items[index] = { ...items[index], ...data }
            localStorage.setItem('mockGeneralAssemblies', JSON.stringify(items))
            return items[index]
        }
        throw new Error('Not found')
    },
    archiveGeneralAssembly: async (id: string) => {
        const items = JSON.parse(localStorage.getItem('mockGeneralAssemblies') || '[]')
        const newItems = items.filter((x: any) => x.id !== id)
        localStorage.setItem('mockGeneralAssemblies', JSON.stringify(newItems))
        return true
    },
}

export default PropertyManagementService
