import { api, projectUrl, metricsUrl } from './client'


//  Types (mirror backend DTOs)


export interface MetricPoint   { date: string; value: number }
export interface RetentionPoint{ day: number;  rate: number  }

export interface DauWauMauResponse {
  dau: MetricPoint[]
  wau: MetricPoint[]
  mau: MetricPoint[]
}

export interface RevenueMetricsResponse {
  dailyRevenue:        MetricPoint[]
  totalRevenue:        number
  arpu:                number
  arppu:               number
  payingUsers:         number
  payerConversionRate: number
}

export interface CohortRow {
  cohortLabel:    string
  cohortSize:     number
  retentionRates: (number | null)[]
}

export interface RetentionResponse {
  classic:     RetentionPoint[]
  rolling:     RetentionPoint[]
  cohortTable: CohortRow[]
}

export interface SessionMetricsResponse {
  avgSessionDuration:      MetricPoint[]
  sessionsPerUser:         MetricPoint[]
  overallAvgDurationSeconds: number
}

export interface UserAcquisitionResponse {
  dailyNewUsers:   MetricPoint[]
  totalNewUsers:   number
  byCountry:       { label: string; value: number }[]
  byPlatform:      { label: string; value: number }[]
}


export interface ProjectResponse {
  id: string; name: string; description: string; apiKey: string
}

export interface DashboardResponse {
  id: string; name: string; description: string; projectId: string
  reports: { id: string; name: string; chartType: string }[]
}

export interface ReportResponse {
  id: string; name: string; description: string
  sqlRequest: string; builderJson: string | null
  chartType: string; projectId: string; dashboardId: string | null
}

export interface ReportResult {
  columns: string[]
  rows: Record<string, unknown>[]
  totalRows: number
  executionMs: number
}

export interface EventModelResponse {
  id: string; name: string; description: string
  eventType: string; isEnabled: boolean; projectId: string
  parameters: ParameterResponse[]
}

export interface ParameterResponse {
  id: string; name: string; parameterType: string
  dataType: string; projectId: string; eventModelId: string | null
}


//  Date range helper


export function toIso(d: Date) { return d.toISOString() }

export function last30Days() {
  const to   = new Date()
  const from = new Date(); from.setDate(from.getDate() - 30)
  return { from: toIso(from), to: toIso(to) }
}


//  Metrics API

export const metricsApi = {
  getDauWauMau: (pid: string, from: string, to: string) =>
    api.get<DauWauMauResponse>(metricsUrl(pid, 'dau-wau-mau', { from, to })),

  getRevenue: (pid: string, from: string, to: string) =>
    api.get<RevenueMetricsResponse>(metricsUrl(pid, 'revenue', { from, to })),

  getRetention: (pid: string, from: string, to: string) =>
    api.get<RetentionResponse>(metricsUrl(pid, 'retention', { from, to })),

  getSessions: (pid: string, from: string, to: string) =>
    api.get<SessionMetricsResponse>(metricsUrl(pid, 'sessions', { from, to })),

    getUserAcquisition: (pid: string, from: string, to: string) =>
      api.get<UserAcquisitionResponse>(metricsUrl(pid, 'user-acquisition', { from, to })),
}

//  Projects API

export const projectsApi = {
  list: (userId: string) =>
    api.get<ProjectResponse[]>(`/api/projects?userId=${userId}`),

  get: (id: string) =>
    api.get<ProjectResponse>(projectUrl(id)),

  create: (userId: string, body: { name: string; description: string }) =>
    api.post<ProjectResponse>(`/api/projects?userId=${userId}`, body),

  update: (id: string, body: { name: string; description: string }) =>
    api.put<ProjectResponse>(projectUrl(id), body),

  delete: (id: string) =>
    api.delete<void>(projectUrl(id)),
}


//  Dashboards API


export const dashboardsApi = {
  list: (pid: string) =>
    api.get<DashboardResponse[]>(projectUrl(pid, '/dashboards')),

  get: (pid: string, id: string) =>
    api.get<DashboardResponse>(projectUrl(pid, `/dashboards/${id}`)),

  create: (pid: string, body: { name: string; description: string }) =>
    api.post<DashboardResponse>(projectUrl(pid, '/dashboards'), body),

  update: (pid: string, id: string, body: { name: string; description: string }) =>
    api.put<DashboardResponse>(projectUrl(pid, `/dashboards/${id}`), body),

  delete: (pid: string, id: string) =>
    api.delete<void>(projectUrl(pid, `/dashboards/${id}`)),
}


//  Reports API


export const reportsApi = {
  list: (pid: string, dashboardId?: string) => {
    const qs = dashboardId ? `?dashboardId=${dashboardId}` : ''
    return api.get<ReportResponse[]>(projectUrl(pid, `/reports${qs}`))
  },

  get: (pid: string, id: string) =>
    api.get<ReportResponse>(projectUrl(pid, `/reports/${id}`)),

  create: (pid: string, body: Partial<ReportResponse>) =>
    api.post<ReportResponse>(projectUrl(pid, '/reports'), body),

  update: (pid: string, id: string, body: Partial<ReportResponse>) =>
    api.put<ReportResponse>(projectUrl(pid, `/reports/${id}`), body),

  delete: (pid: string, id: string) =>
    api.delete<void>(projectUrl(pid, `/reports/${id}`)),

  execute: (pid: string, id: string, overrides?: { sqlOverride?: string; builderJson?: string }) =>
    api.post<ReportResult>(projectUrl(pid, `/reports/${id}/execute`), overrides ?? {}),

  preview: (pid: string, body: { sqlOverride?: string; builderJson?: string }) =>
    api.post<ReportResult>(projectUrl(pid, '/reports/preview'), body),

  sqlPreview: (pid: string, builderJson: string) =>
    api.get<{ sql: string }>(projectUrl(pid, `/reports/builder/sql-preview?builderJson=${encodeURIComponent(builderJson)}`)),
}


//  Event Models API


export const eventModelsApi = {
  list: (pid: string) =>
    api.get<EventModelResponse[]>(projectUrl(pid, '/event-models')),

  get: (pid: string, id: string) =>
    api.get<EventModelResponse>(projectUrl(pid, `/event-models/${id}`)),

  create: (pid: string, body: Partial<EventModelResponse>) =>
    api.post<EventModelResponse>(projectUrl(pid, '/event-models'), body),

  update: (pid: string, id: string, body: Partial<EventModelResponse>) =>
    api.put<EventModelResponse>(projectUrl(pid, `/event-models/${id}`), body),

  delete: (pid: string, id: string) =>
    api.delete<void>(projectUrl(pid, `/event-models/${id}`)),

  addParameter: (pid: string, modelId: string, body: Partial<ParameterResponse>) =>
    api.post<ParameterResponse>(projectUrl(pid, `/event-models/${modelId}/parameters`), body),

  deleteParameter: (pid: string, modelId: string, paramId: string) =>
    api.delete<void>(projectUrl(pid, `/event-models/${modelId}/parameters/${paramId}`)),
}


//  Auth API


export const authApi = {
  login:    (username: string, password: string) =>
    api.post<{ token: string; username: string; userId: string }>('/api/auth/login', { username, password }),

  register: (username: string, password: string) =>
    api.post<{ id: string; username: string }>('/api/auth/register', { username, password }),
}

export interface EventBrowseResponse {
  id: string
  eventTimestamp: string
  eventData: string
  eventModelName: string | null
  eventModelId: string | null
  sessionId: string | null
  userId: string | null
}

export interface PagedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export const eventsApi = {
  browse: (
    pid: string,
    params: { eventModelId?: string; from?: string; to?: string; page?: number; pageSize?: number }
  ) => {
    const qs = new URLSearchParams()
    if (params.eventModelId) qs.set('eventModelId', params.eventModelId)
    if (params.from)         qs.set('from', params.from)
    if (params.to)           qs.set('to', params.to)
    if (params.page)         qs.set('page', String(params.page))
    if (params.pageSize)     qs.set('pageSize', String(params.pageSize))
    return api.get<PagedResponse<EventBrowseResponse>>(projectUrl(pid, `/events?${qs}`))
  },
}
