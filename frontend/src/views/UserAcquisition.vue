<template>
  <div>
    <ContentHeader title="User Acquisition" :filters="['Country', 'Platform']" @dateChange="reload" />
    <div class="p-6 space-y-5">
      <div class="grid grid-cols-2 lg:grid-cols-3 gap-4">
        <StatCard v-for="(k, i) in kpis" :key="k.label"
          :label="k.label" :value="k.value" :info="k.info"
          :class="`fade-up fade-up-${i+1}`" />
      </div>

      <div v-if="loading" class="grid grid-cols-1 xl:grid-cols-2 gap-4">
        <div v-for="n in 3" :key="n" class="stat-card h-64 animate-pulse bg-surface-100 rounded-lg" />
      </div>

      <template v-else>
        <!-- Daily new users -->
        <ReportChart
          class="fade-up fade-up-1"
          title="Daily New Users"
          subtitle="Unique players seen for the first time each day"
          type="bar"
          :chart-data="dailyNewUsersChart"
          :height="220"
          info="A new user is a player whose FirstSeen date falls within the selected period."
          :kpis="[{ label:'Total', value: formatNum(store.userAcquisition?.totalNewUsers ?? 0), color: COLORS.emerald }]"
        />

        <!-- Country + Platform breakdown -->
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
          <!-- Top countries -->
          <div class="stat-card fade-up fade-up-2">
            <h3 class="text-sm font-semibold text-ink-900 mb-4">Top Countries</h3>
            <div v-if="!countryRows.length" class="flex flex-col items-center py-8 text-ink-300">
              <Globe :size="24" class="mb-2" />
              <p class="text-xs">No country data available</p>
            </div>
            <div v-else class="space-y-2.5">
              <div v-for="c in countryRows" :key="c.label" class="flex items-center gap-3">
                <span class="text-sm w-6 text-center">{{ flagEmoji(c.label) }}</span>
                <div class="flex-1">
                  <div class="flex justify-between text-xs mb-1">
                    <span class="font-medium text-ink-700">{{ c.label }}</span>
                    <span class="text-ink-400">{{ formatNum(c.value) }}</span>
                  </div>
                  <div class="h-1.5 bg-surface-100 rounded-full overflow-hidden">
                    <div class="h-full rounded-full bg-brand-500"
                      :style="{ width: (c.value / countryRows[0].value * 100) + '%' }" />
                  </div>
                </div>
                <span class="text-xs font-semibold text-ink-600 w-8 text-right">
                  {{ pct(c.value) }}%
                </span>
              </div>
            </div>
          </div>

          <!-- Platform split -->
          <div class="stat-card fade-up fade-up-3">
            <h3 class="text-sm font-semibold text-ink-900 mb-4">Platform Split</h3>
            <div v-if="!platformRows.length" class="flex flex-col items-center py-8 text-ink-300">
              <Layers :size="24" class="mb-2" />
              <p class="text-xs">No platform data available</p>
            </div>
            <div v-else class="flex gap-6 items-center">
              <div class="relative" style="width:120px;height:120px;flex-shrink:0">
                <Doughnut :data="platformDonut" :options="donutOptions" />
                <div class="absolute inset-0 flex flex-col items-center justify-center">
                  <span class="text-lg font-bold text-ink-900">
                    {{ formatNum(store.userAcquisition?.byPlatform[0].value ?? 0) }}
                  </span>
                  <span class="text-[10px] text-ink-400">{{ store.userAcquisition?.byPlatform[0].label ?? 'Platform' }}</span>
                </div>
              </div>
              <div class="space-y-2 flex-1">
                <div v-for="(p, i) in platformRows" :key="p.label"
                  class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="w-2 h-2 rounded-full" :style="{ background: platformColors[i] }"></span>
                    <span class="text-sm text-ink-600">{{ p.label}}</span>
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="text-sm text-ink-600">{{ p.value }}</span>
                    <div class="w-16 h-1.5 bg-surface-100 rounded-full overflow-hidden">
                      <div class="h-full rounded-full"
                        :style="{ width: pct(p.value) + '%', background: platformColors[i] }" />
                    </div>
                    <span class="text-xs font-semibold text-ink-900 w-8">{{ pct(p.value) }}%</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, watch } from 'vue'
import { Doughnut } from 'vue-chartjs'
import { Chart as ChartJS, ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement } from 'chart.js'
import { Globe, Layers } from 'lucide-vue-next'
import ContentHeader from '../components/ContentHeader.vue'
import StatCard from '../components/StatCard.vue'
import ReportChart from '../components/ReportChart.vue'
import { COLORS, dayLabels, randomWalk, makeBarDataset } from '../composables/useChartData.js'
import { useMetricsStore, useProjectStore } from '../stores/index'

ChartJS.register(ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement)

const store = useMetricsStore()
const projectStore = useProjectStore()
const loading = computed(() => store.loading.userAcquisition)

const platformColors = [COLORS.blue, COLORS.teal, COLORS.violet, COLORS.amber, COLORS.rose, COLORS.cyan, COLORS.emerald]

const countryRows = computed(() => store.userAcquisition?.byCountry  ?? [])
const platformRows = computed(() => store.userAcquisition?.byPlatform ?? [])

const totalUsers = computed(() => store.userAcquisition?.totalNewUsers ?? 0)

function formatNum(n) {
  if (!n) return '0'
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (n >= 1_000) return (n / 1_000).toFixed(1) + 'K'
  return String(Math.round(n))
}

function pct(v) {
  return totalUsers.value > 0 ? Math.round(v / totalUsers.value * 100) : 0
}

const FLAGS = {
  US:'🇺🇸', DE:'🇩🇪', BR:'🇧🇷', JP:'🇯🇵', FR:'🇫🇷',
  GB:'🇬🇧', KR:'🇰🇷', CA:'🇨🇦', RU:'🇷🇺', IN:'🇮🇳',
  AU:'🇦🇺', CN:'🇨🇳', MX:'🇲🇽', IT:'🇮🇹', ES:'🇪🇸',
}
function flagEmoji(code) { return FLAGS[code] ?? '–' }

const dailyNewUsersChart = computed(() => {
  const pts = store.userAcquisition?.dailyNewUsers
  if (!pts?.length) return { labels: dayLabels(30), datasets: [makeBarDataset('New Users', randomWalk(30, 0, 0, 0), COLORS.emerald)] }
  return {
    labels: pts.map(p => new Date(p.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })),
    datasets: [makeBarDataset('New Users', pts.map(p => p.value), COLORS.emerald)],
  }
})

const platformDonut = computed(() => ({
  labels: platformRows.value.map(p => p.label),
  datasets: [{
    data: platformRows.value.map(p => p.value),
    backgroundColor: platformColors,
    borderWidth: 0,
    hoverOffset: 4,
  }],
}))

const donutOptions = {
  responsive: true, maintainAspectRatio: false, cutout: '72%',
  plugins: { legend: { display: false }, tooltip: { enabled: true } },
}

const kpis = computed(() => [
  { label: 'New Users',  value: formatNum(totalUsers.value),   info: 'Unique players seen for the first time in the selected period.' },
  { label: 'Countries',  value: String(countryRows.value.length), info: 'Number of distinct countries represented in new users.' },
  { label: 'Platforms',  value: String(platformRows.value.length), info: 'Number of distinct platforms used by new users.' },
])

async function reload() {
  const pid = projectStore.activeProjectId
  if (pid) { const { from, to } = store.dateRange; await store.fetchUserAcquisition(pid, from, to) }
}

onMounted(reload)
watch(() => projectStore.activeProjectId, reload)
watch(() => store.dateRange, reload, { deep: true })
</script>
