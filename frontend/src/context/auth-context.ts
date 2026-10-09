import { createContext } from 'react'
import type { User, UserRole } from '../types'

export interface AuthState {
  token: string
  user: User
}

export interface AuthContextValue {
  auth: AuthState | null
  login: (email: string, password: string) => Promise<User>
  register: (data: { name: string; email: string; password: string; phone: string; role: UserRole }) => Promise<User>
  saveUser: (user: User) => void
  logout: () => void
}

export const authContext = createContext<AuthContextValue | null>(null)