import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  projectsApi, metricsApi, reportsApi, dashboardsApi, eventModelsApi, authApi,
  last30Days,
  type ProjectResponse, type DauWauMauResponse, type RevenueMetricsResponse,
  type RetentionResponse, type SessionMetricsResponse,
  type ReportResponse, type ReportResult,
  type DashboardResponse, type EventModelResponse,
  type UserAcquisitionResponse,
} from '../api/index'


import { api } from '../api/client'

interface LoginResponse {
  token:     string
  username:  string
  userId:    string
  expiresAt: string
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const userId = ref<string | null>(localStorage.getItem('userId'))
  const username = ref<string | null>(localStorage.getItem('username'))
  const expiresAt = ref<string | null>(localStorage.getItem('expiresAt'))

  const isLoggedIn = computed(() => {
    if (!token.value || !expiresAt.value) return false
    return new Date(expiresAt.value) > new Date()
  })

  const tokenExpiresIn = computed(() => {
    if (!expiresAt.value) return 0
    return Math.max(0, new Date(expiresAt.value).getTime() - Date.now())
  })

  function persist(res: LoginResponse) {
    token.value = res.token
    userId.value = res.userId
    username.value = res.username
    expiresAt.value = res.expiresAt
    localStorage.setItem('token', res.token)
    localStorage.setItem('userId', res.userId)
    localStorage.setItem('username', res.username)
    localStorage.setItem('expiresAt', res.expiresAt)
  }

  async function login(u: string, p: string) {
    const res = await api.post<LoginResponse>('/api/auth/login', { username: u, password: p })
    persist(res)
  }

  async function setup(u: string, p: string) {
    const res = await api.post<LoginResponse>('/api/auth/setup', { username: u, password: p })
    persist(res)
  }

  async function checkNeedsSetup(): Promise<boolean> {
    const res = await api.get<{ needsSetup: boolean }>('/api/auth/status')
    return res.needsSetup
  }

  function logout() {
    token.value = null; userId.value = null
    username.value = null; expiresAt.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('userId')
    localStorage.removeItem('username')
    localStorage.removeItem('expiresAt')
    localStorage.removeItem('activeProjectId')
  }

  return { token, userId, username, expiresAt, isLoggedIn, tokenExpiresIn,
           login, setup, logout, checkNeedsSetup }
})

//  Project Store

export const useProjectStore = defineStore('project', () => {
  const projects        = ref<ProjectResponse[]>([])
  const activeProjectId = ref<string | null>(localStorage.getItem('activeProjectId'))
  const loading         = ref(false)
  const error           = ref<string | null>(null)

  const activeProject = computed(() =>
    projects.value.find(p => p.id === activeProjectId.value) ?? null)

  async function fetchProjects(userId: string) {
    loading.value = true; error.value = null
    try {
      projects.value = await projectsApi.list(userId)
      // Auto-select first project if none active
      if (!activeProjectId.value && projects.value.length)
        setActiveProject(projects.value[0].id)
    } catch (e: any) { error.value = e.message }
    finally { loading.value = false }
  }

  function setActiveProject(id: string) {
    activeProjectId.value = id
    localStorage.setItem('activeProjectId', id)
  }

  async function createProject(userId: string, name: string, description: string) {
    const p = await projectsApi.create(userId, { name, description })
    projects.value.push(p)
    setActiveProject(p.id)
    return p
  }

  async function deleteProject(id: string) {
    await projectsApi.delete(id)
    projects.value = projects.value.filter(p => p.id !== id)
    if (activeProjectId.value === id)
      setActiveProject(projects.value[0]?.id ?? '')
  }

  return { projects, activeProjectId, activeProject, loading, error,
           fetchProjects, setActiveProject, createProject, deleteProject }
})

//  Metrics Store

export const useMetricsStore = defineStore('metrics', () => {
  const dauWauMau  = ref<DauWauMauResponse | null>(null)
  const revenue    = ref<RevenueMetricsResponse | null>(null)
  const retention  = ref<RetentionResponse | null>(null)
  const sessions   = ref<SessionMetricsResponse | null>(null)
  const userAcquisition = ref<UserAcquisitionResponse | null>(null)

  const loading = ref({ dauWauMau: false, revenue: false, retention: false, sessions: false, userAcquisition: false })
  const error   = ref<string | null>(null)

  const dateRange = ref(last30Days())

  async function fetchAll(projectId: string) {
    const { from, to } = dateRange.value
    await Promise.all([
      fetchDauWauMau(projectId, from, to),
      fetchRevenue(projectId, from, to),
      fetchRetention(projectId, from, to),
      fetchSessions(projectId, from, to),
      fetchUserAcquisition(projectId, from, to),
    ])
  }

  async function fetchDauWauMau(pid: string, from: string, to: string) {
    loading.value.dauWauMau = true
    try   { dauWauMau.value = await metricsApi.getDauWauMau(pid, from, to) }
    catch (e: any) { error.value = e.message }
    finally { loading.value.dauWauMau = false }
  }

  async function fetchRevenue(pid: string, from: string, to: string) {
    loading.value.revenue = true
    try   { revenue.value = await metricsApi.getRevenue(pid, from, to) }
    catch (e: any) { error.value = e.message }
    finally { loading.value.revenue = false }
  }

  async function fetchRetention(pid: string, from: string, to: string) {
    loading.value.retention = true
    try   { retention.value = await metricsApi.getRetention(pid, from, to) }
    catch (e: any) { error.value = e.message }
    finally { loading.value.retention = false }
  }

  async function fetchSessions(pid: string, from: string, to: string) {
    loading.value.sessions = true
    try   { sessions.value = await metricsApi.getSessions(pid, from, to) }
    catch (e: any) { error.value = e.message }
    finally { loading.value.sessions = false }
  }

    async function fetchUserAcquisition(pid: string, from: string, to: string) {
      loading.value.userAcquisition = true
      try   { userAcquisition.value = await metricsApi.getUserAcquisition(pid, from, to) }
      catch (e: any) { error.value = e.message }
      finally { loading.value.userAcquisition = false }
    }
  

  function setDateRange(from: string, to: string) {
    dateRange.value = { from, to }
    fetchAll(useProjectStore().activeProjectId!)
  }

  return { dauWauMau, revenue, retention, sessions, userAcquisition,
           loading, error, dateRange,
           fetchAll, fetchDauWauMau, fetchRevenue, fetchRetention, fetchSessions, fetchUserAcquisition, setDateRange }
})

//  Reports Store

export const useReportsStore = defineStore('reports', () => {
  const reports    = ref<ReportResponse[]>([])
  const dashboards = ref<DashboardResponse[]>([])
  const loading    = ref(false)
  const error      = ref<string | null>(null)
  const lastResult = ref<ReportResult | null>(null)
  const executing  = ref(false)

  async function fetchDashboards(pid: string) {
    loading.value = true
    try   { dashboards.value = await dashboardsApi.list(pid) }
    catch (e: any) { error.value = e.message }
    finally { loading.value = false }
  }

  async function fetchReports(pid: string, dashboardId?: string) {
    loading.value = true
    try   { reports.value = await reportsApi.list(pid, dashboardId) }
    catch (e: any) { error.value = e.message }
    finally { loading.value = false }
  }

  async function createReport(pid: string, data: Partial<ReportResponse>) {
    const r = await reportsApi.create(pid, data)
    reports.value.push(r)
    return r
  }

  async function updateReport(pid: string, id: string, data: Partial<ReportResponse>) {
    const r = await reportsApi.update(pid, id, data)
    const idx = reports.value.findIndex(x => x.id === id)
    if (idx >= 0) reports.value[idx] = r
    return r
  }

  async function deleteReport(pid: string, id: string) {
    await reportsApi.delete(pid, id)
    reports.value = reports.value.filter(r => r.id !== id)
  }

  async function executeReport(pid: string, id: string) {
    executing.value = true; error.value = null
    try   { lastResult.value = await reportsApi.execute(pid, id) }
    catch (e: any) { error.value = e.message }
    finally { executing.value = false }
    return lastResult.value
  }

  async function previewReport(pid: string, sql?: string, builderJson?: string) {
    executing.value = true; error.value = null
    try   { lastResult.value = await reportsApi.preview(pid, { sqlOverride: sql, builderJson }) }
    catch (e: any) { error.value = e.message; lastResult.value = null }
    finally { executing.value = false }
    return lastResult.value
  }

  async function getSqlPreview(pid: string, builderJson: string) {
    return reportsApi.sqlPreview(pid, builderJson)
  }

  async function createDashboard(pid: string, name: string, description: string) {
    const d = await dashboardsApi.create(pid, { name, description })
    dashboards.value.push(d)
    return d
  }

  async function deleteDashboard(pid: string, id: string) {
    await dashboardsApi.delete(pid, id)
    dashboards.value = dashboards.value.filter(d => d.id !== id)
  }

  return {
    reports, dashboards, loading, error, lastResult, executing,
    fetchDashboards, fetchReports, createReport, updateReport, deleteReport,
    executeReport, previewReport, getSqlPreview, createDashboard, deleteDashboard,
  }
})

//  Event Models Store

export const useEventModelsStore = defineStore('eventModels', () => {
  const eventModels = ref<EventModelResponse[]>([])
  const loading     = ref(false)
  const error       = ref<string | null>(null)

  async function fetchEventModels(pid: string) {
    loading.value = true
    try   { eventModels.value = await eventModelsApi.list(pid) }
    catch (e: any) { error.value = e.message }
    finally { loading.value = false }
  }

  async function createEventModel(pid: string, data: Partial<EventModelResponse>) {
    const m = await eventModelsApi.create(pid, data)
    eventModels.value.push(m)
    return m
  }

  async function toggleEnabled(pid: string, id: string) {
    const m = eventModels.value.find(x => x.id === id)
    if (!m) return
    const updated = await eventModelsApi.update(pid, id, { ...m, isEnabled: !m.isEnabled })
    Object.assign(m, updated)
  }

  async function deleteEventModel(pid: string, id: string) {
    await eventModelsApi.delete(pid, id)
    eventModels.value = eventModels.value.filter(m => m.id !== id)
  }

  async function addParameter(pid: string, modelId: string, data: object) {
    const p = await eventModelsApi.addParameter(pid, modelId, data)
    const m = eventModels.value.find(x => x.id === modelId)
    if (m) m.parameters.push(p)
    return p
  }

  async function deleteParameter(pid: string, modelId: string, paramId: string) {
    await eventModelsApi.deleteParameter(pid, modelId, paramId)
    const m = eventModels.value.find(x => x.id === modelId)
    if (m) m.parameters = m.parameters.filter(p => p.id !== paramId)
  }

  return {
    eventModels, loading, error,
    fetchEventModels, createEventModel, toggleEnabled, deleteEventModel,
    addParameter, deleteParameter,
  }
})
