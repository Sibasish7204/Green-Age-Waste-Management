import { useState, type ReactNode } from 'react'
import { authApi } from '../api/resources'
import type { User, UserRole } from '../types'
import { authContext, type AuthState } from './auth-context'

function loadAuth(): AuthState | null {
  try {
    const value = localStorage.getItem('greenage_auth')
    return value ? (JSON.parse(value) as AuthState) : null
  } catch {
    localStorage.removeItem('greenage_auth')
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(loadAuth)
  const save = (value: AuthState) => {
    localStorage.setItem('greenage_auth', JSON.stringify(value))
    setAuth(value)
  }
  const login = async (email: string, password: string) => {
    const result = await authApi.login(email, password)
    save(result)
    return result.user
  }
  const register = async (data: { name: string; email: string; password: string; phone: string; role: UserRole }) => {
    const result = await authApi.register(data)
    save(result)
    return result.user
  }
  const saveUser = (user: User) => {
    if (auth) save({ ...auth, user })
  }
  const logout = () => {
    localStorage.removeItem('greenage_auth')
    setAuth(null)
  }
  return <authContext.Provider value={{ auth, login, register, saveUser, logout }}>{children}</authContext.Provider>
}