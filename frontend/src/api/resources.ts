import { client } from './client'
import type { AppNotification, AuthResponse, PickupRequest, QrInfo, User, UserRole, WasteCategory } from '../types'

export const authApi = {
  login: async (email: string, password: string) => (await client.post<AuthResponse>('/auth/login', { email, password })).data,
  register: async (data: { name: string; email: string; password: string; phone: string; role: UserRole }) =>
    (await client.post<AuthResponse>('/auth/register', data)).data,
}

export const categoriesApi = async () => (await client.get<WasteCategory[]>('/categories')).data
export const meApi = {
  get: async () => (await client.get<User>('/me')).data,
  update: async (data: { name: string; phone: string; photoUrl: string }) => (await client.put<User>('/me', data)).data,
}

export const requestsApi = {
  mine: async () => (await client.get<PickupRequest[]>('/requests/mine')).data,
  open: async (lat?: number, lng?: number, radiusKm = 10) =>
    (await client.get<PickupRequest[]>('/requests/open', { params: lat === undefined || lng === undefined ? { radiusKm } : { lat, lng, radiusKm } })).data,
  get: async (id: string) => (await client.get<PickupRequest>(`/requests/${id}`)).data,
  create: async (data: { latitude: number; longitude: number; address: string; notes?: string; items: { categoryId: number; quantity: number }[] }) =>
    (await client.post<PickupRequest>('/requests', data)).data,
  accept: async (id: string) => (await client.post<PickupRequest>(`/requests/${id}/accept`)).data,
  qr: async (id: string) => (await client.get<QrInfo>(`/requests/${id}/qr`)).data,
  verify: async (id: string, token: string) => (await client.post<PickupRequest>(`/requests/${id}/verify-qr`, { token })).data,
  complete: async (id: string, items: { categoryId: number; actualQuantity: number }[]) =>
    (await client.post<PickupRequest>(`/requests/${id}/complete`, { items })).data,
  cancel: async (id: string, reason: string) => (await client.post<PickupRequest>(`/requests/${id}/cancel`, { reason })).data,
}

export const notificationsApi = {
  list: async () => (await client.get<AppNotification[]>('/notifications')).data,
  markRead: async (id: string) => client.post(`/notifications/${id}/read`),
}

export const sosApi = async (latitude: number, longitude: number, message?: string) =>
  client.post('/sos', { latitude, longitude, message })