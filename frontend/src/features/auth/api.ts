import { apiFetch } from '../../shared/api/client'

export type User = { id: string; email: string }

export const getMe = () => apiFetch<{ user: User | null }>('/api/auth/me').then((r) => r.user)

export const register = (email: string, password: string) =>
  apiFetch<void>('/api/auth/register', { method: 'POST', body: { email, password } })

export const login = (email: string, password: string, rememberMe: boolean) =>
  apiFetch<void>('/api/auth/login', { method: 'POST', body: { email, password, rememberMe } })

export const logout = () => apiFetch<void>('/api/auth/logout', { method: 'POST' })
