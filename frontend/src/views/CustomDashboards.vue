<template>
  <div class="flex h-full overflow-hidden">
    <!-- Dashboards sidebar -->
    <div class="w-56 bg-white border-r border-surface-200 flex flex-col shrink-0">
      <div class="px-4 pt-4 pb-2 flex items-center justify-between">
        <p class="text-[10px] font-semibold uppercase tracking-widest text-ink-300">Dashboards</p>
        <button class="text-ink-400 hover:text-brand-600 transition-colors" @click="openDashboardModal">
          <Plus :size="14" />
        </button>
      </div>
      <nav class="flex-1 px-2 space-y-0.5 overflow-y-auto">
        <button
          v-for="d in reportsStore.dashboards" :key="d.id"
          class="nav-item w-full justify-between group"
          :class="{ active: activeDashboardId === d.id }"
          @click="activeDashboardId = d.id"
        >
          <div class="flex items-center gap-2 truncate">
            <LayoutDashboard :size="14" class="shrink-0" />
            <span class="truncate">{{ d.name }}</span>
          </div>
          <button
            class="opacity-0 group-hover:opacity-100 text-ink-300 hover:text-red-500 transition-all"
            @click.stop="deleteDashboard(d)"
          ><X :size="12" /></button>
        </button>
        <div v-if="!reportsStore.dashboards.length" class="px-2 py-4 text-xs text-ink-300">No dashboards yet</div>
      </nav>
    </div>

    <!-- Main area -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <div class="px-6 pt-5 pb-4 bg-white border-b border-surface-200 flex items-center justify-between shrink-0">
        <h1 class="text-lg font-bold text-ink-900">
          {{ activeDashboard?.name ?? 'Custom Dashboards' }}
        </h1>
        <button v-if="activeDashboardId" class="btn-primary text-xs" @click="openReportModal">
          <Plus :size="13" /> New Report
        </button>
      </div>

      <div class="flex-1 overflow-y-auto p-6">
        <div v-if="!activeDashboardId" class="flex flex-col items-center justify-center h-full text-ink-300">
          <LayoutDashboard :size="32" class="mb-3" />
          <p class="text-sm">Select or create a dashboard</p>
        </div>

        <div v-else-if="reportsStore.loading" class="grid grid-cols-1 xl:grid-cols-2 gap-4">
          <div v-for="n in 2" :key="n" class="h-64 bg-white rounded-lg border border-surface-200 animate-pulse" />
        </div>

        <div v-else>
          <div v-if="!dashboardReports.length" class="flex flex-col items-center justify-center py-16 text-ink-300">
            <FileBarChart :size="28" class="mb-2" />
            <p class="text-sm">No reports yet — create one above</p>
          </div>

          <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <div v-for="report in dashboardReports" :key="report.id" class="stat-card">
              <div class="flex items-center justify-between mb-3">
                <div>
                  <p class="text-sm font-semibold text-ink-900">{{ report.name }}</p>
                  <p class="text-xs text-ink-400 mt-0.5">{{ report.description }}</p>
                </div>
                <div class="flex gap-1">
                  <button class="btn-ghost p-1.5 text-ink-400" @click="openEditor(report)"><Pencil :size="13" /></button>
                  <button class="btn-ghost p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50" @click="deleteReport(report)"><Trash2 :size="13" /></button>
                </div>
              </div>

              <ReportChart
                v-if="results[report.id]"
                :title="''"
                :type="report.chartType?.toLowerCase() ?? 'bar'"
                :chart-data="toChartData(results[report.id], report.chartType)"
                :height="230"
              />
              <div v-else class="flex items-center justify-center h-32 bg-surface-50 rounded text-ink-300 text-xs gap-2">
                <button class="btn-ghost text-xs" @click="runReport(report)">
                  <Play :size="12" /> Run report
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Report editor drawer -->
    <div v-if="editorOpen" class="w-[480px] border-l border-surface-200 bg-white flex flex-col shrink-0 overflow-hidden">
      <div class="px-4 pt-4 pb-3 border-b border-surface-200 flex items-center justify-between">
        <p class="text-sm font-bold text-ink-900">{{ editingReport ? 'Edit Report' : 'New Report' }}</p>
        <button class="btn-ghost p-1.5 text-ink-400" @click="editorOpen = false"><X :size="15" /></button>
      </div>

      <div class="flex-1 overflow-y-auto p-4 space-y-4">
        <div>
          <label class="block text-xs font-medium text-ink-600 mb-1">Name</label>
          <input v-model="reportForm.name" type="text" placeholder="My report"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
        </div>
        <div>
          <label class="block text-xs font-medium text-ink-600 mb-1">Description</label>
          <input v-model="reportForm.description" type="text" placeholder="Optional"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
        </div>
        <div>
          <label class="block text-xs font-medium text-ink-600 mb-1">Chart Type</label>
          <select v-model="reportForm.chartType"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
            <option v-for="t in chartTypes" :key="t" :value="t">{{ t }}</option>
          </select>
        </div>

        <!-- Mode toggle -->
        <div>
          <div class="flex gap-1 bg-surface-100 rounded p-0.5 mb-3">
            <button v-for="m in ['Builder', 'SQL']" :key="m"
              class="flex-1 py-1.5 text-xs font-medium rounded transition-all"
              :class="editorMode === m ? 'bg-white text-ink-900 shadow-sm' : 'text-ink-400 hover:text-ink-700'"
              @click="editorMode = m"
            >{{ m }}</button>
          </div>

          <!-- Visual builder -->
          <div v-if="editorMode === 'Builder'" class="space-y-3">
              <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Table</label>
              <select v-model="builder.metric" class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option value="events">Events</option>
                <option value="sessions">Sessions</option>
                <option value="users">Users</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Event Model</label>
              <select v-model="builder.eventModelId" class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all"
                @change="builder.valueField = ''; builder.groupByParameter = ''; if (builder.groupBy === 'parameter') builder.groupBy = 'day'">
                <option value="">All events</option>
                <option v-for="m in eventModels" :key="m.id" :value="m.id">{{ m.name }}</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Aggregate</label>
              <select v-model="builder.aggregate" class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option value="count">Count</option>
                <option value="distinct_users">Distinct users</option>
                <option value="sum">Sum of parameter</option>
                <option value="avg">Average of parameter</option>
              </select>
            </div>
            <div v-if="builder.aggregate === 'sum' || builder.aggregate === 'avg'">
              <label class="block text-xs font-medium text-ink-600 mb-1">Parameter (value field)</label>
              <select v-model="builder.valueField" class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option value="">— select —</option>
                <option v-for="p in selectedModelParams" :key="p.name" :value="p.name">{{ p.name }} ({{ p.dataType }})</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Group by</label>
              <select v-model="builder.groupBy" class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all"
                @change="builder.groupByParameter = ''">
                <option value="day">Day</option>
                <option value="week">Week</option>
                <option value="month">Month</option>
                <option value="country">Country</option>
                <option value="platform">Platform</option>
                <option value="parameter" :disabled="!selectedModelParams.length">
                  Parameter{{ !selectedModelParams.length ? ' (select an event model first)' : '' }}
                </option>
              </select>
            </div>

            <div v-if="builder.groupBy === 'parameter' && selectedModelParams.length">
              <label class="block text-xs font-medium text-ink-600 mb-1">Parameter to group by</label>
              <select v-model="builder.groupByParameter" class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option value="">— select —</option>
                <option v-for="p in selectedModelParams" :key="p.name" :value="p.name">
                  {{ p.name }} ({{ p.dataType }})
                </option>
              </select>
            </div>

            <!-- Date range -->
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Date range</label>
              <div class="flex items-center gap-2">
                <input type="date" v-model="builder.from"
                  class="flex-1 px-2 py-1.5 text-xs border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all" />
                <span class="text-ink-300 text-xs">—</span>
                <input type="date" v-model="builder.to"
                  class="flex-1 px-2 py-1.5 text-xs border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all" />
              </div>
              <p class="text-[10px] text-ink-300 mt-1">Leave empty to use all available data</p>
            </div>

            <!-- Generated SQL preview -->
            <div v-if="sqlPreview">
              <label class="block text-xs font-medium text-ink-600 mb-1">Generated SQL</label>
              <pre class="text-[11px] bg-surface-50 border border-surface-200 rounded p-3 overflow-x-auto text-ink-600 font-mono whitespace-pre-wrap">{{ sqlPreview }}</pre>
            </div>
            <button class="btn-ghost text-xs w-full justify-center" @click="previewSql">Preview SQL</button>
          </div>

          <!-- SQL editor -->
          <div v-else>
            <label class="block text-xs font-medium text-ink-600 mb-1">SQL Query</label>
            <textarea
              v-model="reportForm.sqlRequest"
              rows="8"
              placeholder="SELECT DATE_TRUNC('day', &quot;EventTimestamp&quot;) AS label, COUNT(*) AS value FROM &quot;Events&quot; WHERE &quot;ProjectId&quot; = '...' GROUP BY 1 ORDER BY 1"
              class="w-full px-3 py-2 text-xs font-mono border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all resize-none"
            />
          </div>
        </div>

        <!-- Run preview -->
        <div>
          <button class="btn-ghost text-xs w-full justify-center border border-surface-200" :disabled="previewing" @click="previewRun">
            <Loader2 v-if="previewing" :size="12" class="animate-spin" />
            <Play v-else :size="12" />
            Run preview
          </button>

          <div v-if="previewError" class="mt-2 text-xs text-red-500 bg-red-50 rounded p-2">{{ previewError }}</div>

          <div v-if="previewResult" class="mt-3">
            <ReportChart
              title="Preview"
              :type="reportForm.chartType?.toLowerCase() ?? 'bar'"
              :chart-data="toChartData(previewResult, reportForm.chartType)"
              :height="140"
            />
            <p class="text-[10px] text-ink-300 mt-1 text-right">{{ previewResult.totalRows }} rows · {{ previewResult.executionMs }}ms</p>
          </div>
        </div>
      </div>

      <div class="px-4 py-3 border-t border-surface-200 flex gap-2">
        <button class="btn-primary text-xs" :disabled="saving" @click="saveReport">
          <Loader2 v-if="saving" :size="13" class="animate-spin" /> Save
        </button>
        <button class="btn-ghost text-xs" @click="editorOpen = false">Cancel</button>
      </div>
    </div>

    <!-- Dashboard create modal -->
    <div v-if="dashboardModal" class="fixed inset-0 bg-black/20 z-50 flex items-center justify-center" @click.self="dashboardModal = false">
      <div class="bg-white rounded-lg shadow-float w-full max-w-sm p-5">
        <h2 class="text-sm font-bold text-ink-900 mb-4">New Dashboard</h2>
        <div class="space-y-3">
          <input v-model="dashForm.name" type="text" placeholder="Dashboard name"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
          <input v-model="dashForm.description" type="text" placeholder="Description (optional)"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
        </div>
        <div class="flex gap-2 mt-4">
          <button class="btn-primary text-xs" :disabled="!dashForm.name || savingDash" @click="createDashboard">
            <Loader2 v-if="savingDash" :size="13" class="animate-spin" /> Create
          </button>
          <button class="btn-ghost text-xs" @click="dashboardModal = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, watchEffect } from 'vue'
import { Plus, X, Trash2, Pencil, Play, Loader2, LayoutDashboard, FileBarChart } from 'lucide-vue-next'
import ReportChart from '../components/ReportChart.vue'
import { useReportsStore, useProjectStore, useEventModelsStore } from '../stores/index'
import { COLORS, makeLineDataset, makeBarDataset } from '../composables/useChartData.js'

const reportsStore = useReportsStore()
const projectStore = useProjectStore()
const eventModelsStore = useEventModelsStore()
const projectId = computed(() => projectStore.activeProjectId)

const eventModels = computed(() => eventModelsStore.eventModels)
const selectedModelParams = computed(() => {
  if (!builder.value.eventModelId) return []
  const m = eventModels.value.find(m => m.id === builder.value.eventModelId)
  return m?.parameters ?? []
})

const activeDashboardId = ref(null)
const activeDashboard = computed(() => reportsStore.dashboards.find(d => d.id === activeDashboardId.value))
const dashboardReports = computed(() => reportsStore.reports.filter(r => r.dashboardId === activeDashboardId.value))

const results = ref({})
const editorOpen = ref(false)
const editingReport = ref(null)
const editorMode = ref('Builder')
const previewing = ref(false)
const previewResult = ref(null)
const previewError = ref('')
const saving = ref(false)
const sqlPreview = ref('')

const dashboardModal = ref(false)
const savingDash = ref(false)
const dashForm = ref({ name: '', description: '' })

const chartTypes = ['Bar', 'Line', 'Doughnut', 'Table']

const builder = ref({ eventModelId: '', aggregate: 'count', groupBy: 'day', valueField: '', groupByParameter: '', from: '', to: '' })

const reportForm = ref({ name: '', description: '', chartType: 'Bar', sqlRequest: '', builderJson: null })

function openDashboardModal() {
  dashForm.value = { name: '', description: '' }
  dashboardModal.value = true
}

async function createDashboard() {
  savingDash.value = true
  try {
    const d = await reportsStore.createDashboard(projectId.value, dashForm.value.name, dashForm.value.description)
    activeDashboardId.value = d.id
    dashboardModal.value = false
  } finally { savingDash.value = false }
}

async function deleteDashboard(d) {
  if (!confirm(`Delete "${d.name}"?`)) return
  await reportsStore.deleteDashboard(projectId.value, d.id)
  if (activeDashboardId.value === d.id) activeDashboardId.value = null
}

function openReportModal() {
  editingReport.value = null
  editorMode.value = 'Builder'
  reportForm.value = { name: '', description: '', chartType: 'Bar', sqlRequest: '' }
  builder.value = { eventModelId: '', aggregate: 'count', groupBy: 'day', valueField: '', groupByParameter: '', from: '', to: '' }
  previewResult.value = null
  previewError.value = ''
  sqlPreview.value = ''
  editorOpen.value = true
}

function openEditor(report) {
  editingReport.value = report
  reportForm.value = { ...report }
  if (report.builderJson) {
    try { builder.value = { eventModelId: '', aggregate: 'count', groupBy: 'day', valueField: '', groupByParameter: '', from: '', to: '', ...JSON.parse(report.builderJson) } }
    catch {}
    editorMode.value = 'Builder'
  } else {
    editorMode.value = 'SQL'
  }
  previewResult.value = null
  previewError.value = ''
  sqlPreview.value = ''
  editorOpen.value = true
}

async function previewSql() {
  const bj = JSON.stringify(builder.value)
  const res = await reportsStore.getSqlPreview(projectId.value, bj)
  sqlPreview.value = res?.sql ?? ''
}

async function previewRun() {
  previewing.value = true
  previewError.value = ''
  previewResult.value = null
  try {
    const bj = editorMode.value === 'Builder' ? JSON.stringify(builder.value) : null
    const sql = editorMode.value === 'SQL' ? reportForm.value.sqlRequest : null
    previewResult.value = await reportsStore.previewReport(projectId.value, sql, bj)
  } catch (e) { previewError.value = e.message }
  finally { previewing.value = false }
}

async function saveReport() {
  saving.value = true
  try {
    const data = {
      ...reportForm.value,
      dashboardId: activeDashboardId.value,
      builderJson: editorMode.value === 'Builder' ? JSON.stringify(builder.value) : null,
      sqlRequest:  editorMode.value === 'SQL' ? reportForm.value.sqlRequest : '',
    }
    if (editingReport.value) {
      await reportsStore.updateReport(projectId.value, editingReport.value.id, data)
    } else {
      await reportsStore.createReport(projectId.value, data)
    }
    editorOpen.value = false
    await reportsStore.fetchReports(projectId.value, activeDashboardId.value)
  } finally { saving.value = false }
}

async function deleteReport(report) {
  if (!confirm(`Delete "${report.name}"?`)) return
  await reportsStore.deleteReport(projectId.value, report.id)
  delete results.value[report.id]
}

async function runReport(report) {
  const res = await reportsStore.executeReport(projectId.value, report.id)
  if (res) results.value[report.id] = res
}

function toChartData(result, chartType) {
  if (!result?.rows?.length) return null
  const labels = result.rows.map(r => String(r.label ?? r[result.columns[0]]))
  const values = result.rows.map(r => Number(r.value ?? r[result.columns[1]] ?? 0))
  const type = chartType?.toLowerCase()
  if (type === 'doughnut') {
    return { labels, datasets: [{ data: values, backgroundColor: [COLORS.blue, COLORS.teal, COLORS.violet, COLORS.amber, COLORS.rose], borderWidth: 0 }] }
  }
  if (type === 'line') {
    return { labels, datasets: [makeLineDataset('value', values, COLORS.blue, COLORS.blueFade)] }
  }
  return { labels, datasets: [makeBarDataset('value', values, COLORS.blue)] }
}

async function load() {
  if (!projectId.value) return
  await Promise.all([
    reportsStore.fetchDashboards(projectId.value),
    reportsStore.fetchReports(projectId.value),
    eventModelsStore.fetchEventModels(projectId.value),
  ])
  if (reportsStore.dashboards.length && !activeDashboardId.value) {
    activeDashboardId.value = reportsStore.dashboards[0].id
  }
}

onMounted(load)
watch(() => projectStore.activeProjectId, load)
watch(() => builder.value.groupBy, (gby) => {
  const now = new Date()
  const to = now.toISOString().slice(0, 10)
  if (gby === 'month') {
    const from = new Date(now); from.setMonth(from.getMonth() - 6)
    builder.value.from = from.toISOString().slice(0, 10)
    builder.value.to = to
  } else if (gby === 'week') {
    const from = new Date(now); from.setDate(from.getDate() - 90)
    builder.value.from = from.toISOString().slice(0, 10)
    builder.value.to = to
  }
})
watch(activeDashboardId, (id) => {
  if (id && projectId.value) reportsStore.fetchReports(projectId.value, id)
})
</script>

