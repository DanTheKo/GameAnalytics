import type { ApiResponse } from './types'


export const API_CONFIG = {
  baseURL: 'http://localhost:5067',
  timeout: 10000,
  useMock: false, 
}

// Simple fetch wrapper with error handling
export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const url = `${API_CONFIG.baseURL}${endpoint}`
  
  const defaultHeaders: HeadersInit = {
    'Content-Type': 'application/json',
  }

  const config: RequestInit = {
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  }

  try {
    const response = await fetch(url, config)
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const result = await response.json()
    return {
      data: result.data || result,
      success: true,
    }
  } catch (error) {
    console.error('API Request failed:', error)
    return {
      data: null as unknown as T,
      success: false,
      message: error instanceof Error ? error.message : 'Unknown error',
    }
  }
}

export async function apiPost<T, B = unknown>(
  endpoint: string,
  body: B
): Promise<ApiResponse<T>> {
  return apiRequest<T>(endpoint, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export async function apiGet<T>(
  endpoint: string,
  params?: Record<string, unknown>
): Promise<ApiResponse<T>> {
  const validParams = params 
    ? Object.fromEntries(
        Object.entries(params)
          .filter(([_, v]) => v !== undefined && v !== null)
          .map(([k, v]) => [k, String(v)])
      )
    : {}
  
  const queryString = Object.keys(validParams).length > 0
    ? '?' + new URLSearchParams(validParams).toString()
    : ''
  
  return apiRequest<T>(`${endpoint}${queryString}`)
}

