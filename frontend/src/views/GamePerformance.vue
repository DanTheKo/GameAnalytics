<template>
  <div>
    <ContentHeader title="Game Performance" />

    <div class="p-6 space-y-5">
      <!-- KPI row -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          v-for="(kpi, i) in kpis"
          :key="kpi.label"
          :label="kpi.label"
          :value="kpi.value"
          :info="kpi.info"
          :class="`fade-up fade-up-${i + 1}`"
        />
      </div>

      <!-- Loading skeleton -->
      <div v-if="isLoading" class="grid grid-cols-1 xl:grid-cols-2 gap-4">
        <div v-for="n in 4" :key="n" class="stat-card h-64 animate-pulse bg-surface-100 rounded-lg" />
      </div>

      <template v-else>
        <!-- Charts row 1 -->
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
          <ReportChart
            class="fade-up fade-up-1"
            title="Daily Active Users (DAU)"
            info="Number of unique users who started at least one session on a given day."
            subtitle="Unique users with sessions per day"
            type="line"
            :chart-data="dauChart"
            :height="200"
            :kpis="[{ label:'Latest DAU', value: formatNum(latestDau), color: COLORS.blue }]"
          />
          <ReportChart
            class="fade-up fade-up-2"
            title="Weekly Active Users (WAU)"
            info="Unique users active in a rolling 7-day window ending on each day."
            subtitle="Rolling 7-day unique active users"
            type="line"
            :chart-data="wauChart"
            :height="200"
            :kpis="[{ label:'Latest WAU', value: formatNum(latestWau), color: COLORS.teal }]"
          />
        </div>

        <!-- Charts row 2 -->
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
          <ReportChart
            class="fade-up fade-up-3"
            title="Monthly Active Users (MAU)"
            info="Unique users active in a rolling 30-day window ending on each day."
            subtitle="Rolling 30-day unique active users"
            type="line"
            :chart-data="mauChart"
            :height="200"
            :kpis="[{ label:'Latest MAU', value: formatNum(latestMau), color: COLORS.violet }]"
          />
          <ReportChart
            class="fade-up fade-up-4"
            title="Avg Session Duration"
            info="Average number of seconds players spent in a session per day."
            subtitle="Average seconds per session per day"
            type="line"
            :chart-data="sessionDurationChart"
            :height="200"
            :kpis="[{ label:'Overall avg', value: formatDuration(metricsStore.sessions?.overallAvgDurationSeconds ?? 0), color: COLORS.emerald }]"
          />
        </div>

        <!-- Charts row 3 -->
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
          <ReportChart
            class="fade-up fade-up-5"
            title="Sessions per User"
            info="Average number of sessions started per unique active user per day."
            type="bar"
            :chart-data="sessionsPerUserChart"
            :height="180"
            :kpis="[{ label:'Latest Avg', value: latestSessionsPerUser.toFixed(1), color: COLORS.amber }]"
          />

          <!-- Revenue mini -->
          <ReportChart
            class="fade-up fade-up-6"
            title="Daily Revenue"
            info="Total in-app purchase revenue recorded per day, summed from Revenue-type events."
            type="bar"
            :chart-data="revenueChart"
            :height="180"
            :kpis="[{ label:'Total', value: formatMoney(metricsStore.revenue?.totalRevenue ?? 0), color: COLORS.rose }]"
          />

          <!-- Platform donut 
          <div class="stat-card fade-up fade-up-6">
            <div class="flex items-center justify-between mb-4">
              <h3 class="text-sm font-semibold text-ink-900">Platform Split</h3>
            </div>
            <div class="flex gap-6 items-center">
              <div class="relative" style="width:110px;height:110px;flex-shrink:0">
                <Doughnut :data="platformData" :options="donutOptions" />
                <div class="absolute inset-0 flex flex-col items-center justify-center">
                  <span class="text-xl font-bold text-ink-900">64%</span>
                  <span class="text-[10px] text-ink-400">iOS</span>
                </div>
              </div>
              <div class="space-y-2 flex-1">
                <div v-for="p in platforms" :key="p.label" class="flex items-center justify-between">
                  <div class="flex items-center gap-1.5">
                    <span class="w-2 h-2 rounded-full" :style="{ background: p.color }"></span>
                    <span class="text-xs text-ink-600">{{ p.label }}</span>
                  </div>
                  <span class="text-xs font-semibold text-ink-900">{{ p.pct }}%</span>
                </div>
              </div>
            </div>
          </div>-->
        </div>
      </template>

      <!-- Error banner -->
      <div v-if="metricsStore.error" class="stat-card border border-red-100 bg-red-50">
        <div class="flex items-center gap-3">
          <AlertCircle :size="18" class="text-red-500 shrink-0" />
          <div>
            <p class="text-sm font-semibold text-red-700">Failed to load metrics</p>
            <p class="text-xs text-red-500 mt-0.5">{{ metricsStore.error }}</p>
          </div>
          <button class="btn-ghost ml-auto text-xs text-red-600 hover:bg-red-100" @click="reload">
            Retry
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, watch } from 'vue'
import { Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS, ArcElement, Tooltip, Legend,
  CategoryScale, LinearScale, PointElement, LineElement, BarElement, Filler
} from 'chart.js'
import { AlertCircle } from 'lucide-vue-next'
import ContentHeader from '../components/ContentHeader.vue'
import StatCard from '../components/StatCard.vue'
import ReportChart from '../components/ReportChart.vue'
import { COLORS, dayLabels, randomWalk, makeLineDataset, makeBarDataset } from '../composables/useChartData.js'
import { useMetricsStore, useProjectStore } from '../stores/index'

ChartJS.register(ArcElement, Tooltip, Legend, CategoryScale, LinearScale, PointElement, LineElement, BarElement, Filler)

const metricsStore = useMetricsStore()
const projectStore = useProjectStore()

const isLoading = computed(() =>
  metricsStore.loading.dauWauMau || metricsStore.loading.sessions
)

// Formatters

function formatNum(n) {
  if (!n) return '—'
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (n >= 1_000)     return (n / 1_000).toFixed(1) + 'K'
  return String(Math.round(n))
}

function formatDuration(secs) {
  if (!secs) return '—'
  const m = Math.floor(secs / 60)
  const s = Math.round(secs % 60)
  return `${m}m ${s}s`
}

function formatMoney(v) {
  if (!v) return '$0'
  return '$' + (v >= 1000 ? (v / 1000).toFixed(1) + 'K' : v.toFixed(0))
}

function toLabels(points) {
  return (points ?? []).map(p => {
    const d = new Date(p.date)
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  })
}

// Derived latest values

const latestDau = computed(() => {
  const pts = metricsStore.dauWauMau?.dau
  return pts?.length ? pts[pts.length - 1].value : 0
})
const latestWau = computed(() => {
  const pts = metricsStore.dauWauMau?.wau
  return pts?.length ? pts[pts.length - 1].value : 0
})
const latestMau = computed(() => {
  const pts = metricsStore.dauWauMau?.mau
  return pts?.length ? pts[pts.length - 1].value : 0
})
const latestSessionsPerUser = computed(() => {
  const pts = metricsStore.sessions?.sessionsPerUser
  return pts?.length ? pts[pts.length - 1].value : 0
})

// Chart data (real → fallback mock)

const labels30 = dayLabels(30)

function fallbackLine(color, fade) {
  return { labels: labels30, datasets: [makeLineDataset('—', randomWalk(30, 0, 0, 0), color, fade)] }
}
function fallbackBar(color) {
  return { labels: labels30, datasets: [makeBarDataset('—', randomWalk(30, 0, 0, 0), color)] }
}

const dauChart = computed(() => {
  const pts = metricsStore.dauWauMau?.dau
  if (!pts?.length) return fallbackLine(COLORS.blue, COLORS.blueFade)
  return { labels: toLabels(pts), datasets: [makeLineDataset('DAU', pts.map(p => p.value), COLORS.blue, COLORS.blueFade)] }
})

const wauChart = computed(() => {
  const pts = metricsStore.dauWauMau?.wau
  if (!pts?.length) return fallbackLine(COLORS.teal, COLORS.tealFade)
  return { labels: toLabels(pts), datasets: [makeLineDataset('WAU', pts.map(p => p.value), COLORS.teal, COLORS.tealFade)] }
})

const mauChart = computed(() => {
  const pts = metricsStore.dauWauMau?.mau
  if (!pts?.length) return fallbackLine(COLORS.violet, COLORS.violetFade)
  return { labels: toLabels(pts), datasets: [makeLineDataset('MAU', pts.map(p => p.value), COLORS.violet, COLORS.violetFade)] }
})

const sessionDurationChart = computed(() => {
  const pts = metricsStore.sessions?.avgSessionDuration
  if (!pts?.length) return fallbackLine(COLORS.emerald, 'rgba(16,185,129,0.10)')
  return { labels: toLabels(pts), datasets: [makeLineDataset('Avg Sec', pts.map(p => p.value), COLORS.emerald, 'rgba(16,185,129,0.10)')] }
})

const sessionsPerUserChart = computed(() => {
  const pts = metricsStore.sessions?.sessionsPerUser
  if (!pts?.length) return fallbackBar(COLORS.amber)
  return { labels: toLabels(pts), datasets: [makeBarDataset('Sessions', pts.map(p => p.value), COLORS.amber)] }
})

const revenueChart = computed(() => {
  const pts = metricsStore.revenue?.dailyRevenue
  if (!pts?.length) return fallbackBar(COLORS.rose)
  return { labels: toLabels(pts), datasets: [makeBarDataset('Revenue', pts.map(p => p.value), COLORS.rose)] }
})

// KPI summary cards

const kpis = computed(() => [
  { label: 'DAU', value: formatNum(latestDau.value),  info: 'Daily Active Users — unique users with at least one session today.' },
  { label: 'WAU', value: formatNum(latestWau.value),  info: 'Weekly Active Users — unique users active in the last 7 days.' },
  { label: 'MAU', value: formatNum(latestMau.value),  info: 'Monthly Active Users — unique users active in the last 30 days.' },
  { label: 'Avg Session', value: formatDuration(metricsStore.sessions?.overallAvgDurationSeconds ?? 0), info: 'Average session duration across all sessions in the selected period.' },
])

// Platform donut (static — would come from a breakdown endpoint)

const platforms = [
  { label: 'iOS',     pct: 64, color: COLORS.blue   },
  { label: 'Android', pct: 29, color: COLORS.teal   },
  { label: 'WebGL',   pct:  7, color: COLORS.violet },
]
const platformData = {
  datasets: [{ data: [64, 29, 7], backgroundColor: [COLORS.blue, COLORS.teal, COLORS.violet], borderWidth: 0 }],
}
const donutOptions = {
  responsive: true, maintainAspectRatio: false, cutout: '72%',
  plugins: { legend: { display: false }, tooltip: { enabled: false } },
}

// Data loading

async function reload() {
  const pid = projectStore.activeProjectId
  if (pid) await metricsStore.fetchAll(pid)
}

onMounted(reload)
watch(() => projectStore.activeProjectId, reload)
watch(() => metricsStore.dateRange, reload, { deep: true })
</script>
