/// <reference types="vite/client" />

const BASE_URL = import.meta.env.VITE_API_URL || ''

export class ApiError extends Error {
  constructor(public status: number, message: string, public detail?: string) {
    super(message)
  }
}

function getToken(): string | null {
  return localStorage.getItem('token')
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> ?? {}),
  }

  const token = getToken()
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(`${BASE_URL}${path}`, { ...options, headers })

  if (res.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('userId')
    localStorage.removeItem('username')
    window.location.hash = '#/login'
    throw new ApiError(401, 'Session expired. Please log in again.')
  }

  if (!res.ok) {
    let message = `HTTP ${res.status}`
    let detail: string | undefined
    try { const b = await res.json(); message = b.message ?? message; detail = b.detail }
    catch {}
    throw new ApiError(res.status, message, detail)
  }

  if (res.status === 204) return undefined as T
  return res.json()
}

export const api = {
  get:    <T>(path: string) => request<T>(path, { method: 'GET' }),
  post:   <T>(path: string, body: unknown) => request<T>(path, { method: 'POST',  body: JSON.stringify(body) }),
  put:    <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT',   body: JSON.stringify(body) }),
  delete: <T>(path: string) =>               request<T>(path, { method: 'DELETE' }),
}

export function projectUrl(projectId: string, suffix = '') {
  return `/api/projects/${projectId}${suffix}`
}

export function metricsUrl(projectId: string, endpoint: string, params: Record<string, string>) {
  const qs = new URLSearchParams(params).toString()
  return `${projectUrl(projectId, `/metrics/${endpoint}`)}?${qs}`
}
