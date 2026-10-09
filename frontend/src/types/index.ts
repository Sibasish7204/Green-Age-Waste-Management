export type UserRole = 'Deliver' | 'Collect'
export type WasteGroup = 'Bio' | 'NonBio'
export type RequestStatus = 'Requested' | 'Accepted' | 'Verified' | 'Completed' | 'Cancelled'

export interface User {
  id: string
  name: string
  email: string
  phone?: string | null
  role: UserRole
  photoUrl?: string | null
}

export interface AuthResponse {
  token: string
  user: User
}

export interface WasteCategory {
  id: number
  name: string
  group: WasteGroup
  unit: string
  iconKey: string
}

export interface RequestItem {
  categoryId: number
  categoryName: string
  unit: string
  requestedQuantity: number
  actualQuantity?: number | null
}

export interface PartyInfo {
  id: string
  name: string
  phone?: string | null
}

export interface PickupRequest {
  id: string
  status: RequestStatus
  latitude: number
  longitude: number
  address: string
  notes?: string | null
  deliver: PartyInfo
  collector?: PartyInfo | null
  items: RequestItem[]
  distanceKm?: number | null
  createdAt: string
  acceptedAt?: string | null
  verifiedAt?: string | null
  completedAt?: string | null
  cancelReason?: string | null
}

export interface QrInfo {
  requestId: string
  token: string
  expiresAt: string
  payload: string
}

export interface AppNotification {
  id: string
  title: string
  message: string
  requestId?: string | null
  isRead: boolean
  createdAt: string
}

export interface RequestDraft {
  latitude: number
  longitude: number
  address: string
  selectedCategoryIds: number[]
}