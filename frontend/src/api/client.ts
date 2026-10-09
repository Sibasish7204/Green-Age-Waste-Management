import axios from 'axios'

export const client = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api',
  headers: { 'Content-Type': 'application/json' },
})

client.interceptors.request.use((config) => {
  const auth = localStorage.getItem('greenage_auth')
  if (auth) {
    const parsed = JSON.parse(auth) as { token?: string }
    if (parsed.token) config.headers.Authorization = `Bearer ${parsed.token}`
  }
  return config
})

client.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      localStorage.removeItem('greenage_auth')
      if (window.location.pathname !== '/login') window.location.assign('/login')
    }
    return Promise.reject(error)
  },
)

export function errorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const problem = error.response?.data as { detail?: string; title?: string } | undefined
    return problem?.detail || problem?.title || error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong.'
}