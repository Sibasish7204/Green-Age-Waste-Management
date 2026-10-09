import { useContext } from 'react'
import { authContext } from './auth-context'

export function useAuth() {
  const value = useContext(authContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}