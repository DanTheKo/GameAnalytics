<template>
  <div>
    <ContentHeader title="Retention" :filters="['Country', 'Platform', 'Version', 'Cohort Size']" />
    <div class="p-6 space-y-5">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard v-for="(k, i) in kpis" :key="k.label" :label="k.label" :value="k.value" :class="`fade-up fade-up-${i+1}`" />
      </div>

      <div v-if="loading" class="grid grid-cols-1 xl:grid-cols-2 gap-4">
        <div v-for="n in 2" :key="n" class="stat-card h-64 animate-pulse bg-surface-100 rounded-lg" />
      </div>

      <template v-else>
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
          <ReportChart class="fade-up fade-up-1" title="Classic Retention by Day"
            info="% of users from a cohort who returned on exactly day N after install." subtitle="% of users returning on exactly day N"
            type="line" :chart-data="classicChart" :height="220"
            :kpis="[ { label:'D1', value: getRate('classic',1)+'%', color: COLORS.blue }, { label:'D7', value: getRate('classic',7)+'%', color: COLORS.teal }, { label:'D30', value: getRate('classic',30)+'%', color: COLORS.violet } ]" />
          <ReportChart class="fade-up fade-up-2" title="Rolling Retention"
            info="% of users who were active on day N or any day after — a less strict measure of long-term engagement." subtitle="% of users active on or after day N"
            type="line" :chart-data="rollingChart" :height="220"
            :kpis="[ { label:'D7', value: getRate('rolling',7)+'%', color: COLORS.blue }, { label:'D30', value: getRate('rolling',30)+'%', color: COLORS.amber } ]" />
        </div>

        <div class="stat-card fade-up fade-up-3">
          <h3 class="text-sm font-semibold text-ink-900 mb-4">Cohort Retention Heatmap</h3>
          <div v-if="!cohortRows.length" class="flex flex-col items-center py-10 text-ink-300">
            <p class="text-sm">No cohort data for this period</p>
          </div>
          <div v-else class="overflow-x-auto">
            <table class="w-full text-xs">
              <thead>
                <tr>
                  <th class="text-left text-ink-400 font-medium pb-2 pr-4">Cohort</th>
                  <th class="text-center text-ink-400 font-medium pb-2 px-1">Size</th>
                  <th v-for="d in ['D1','D7','D14','D30','D60','D90']" :key="d" class="text-center text-ink-400 font-medium pb-2 px-1">{{ d }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in cohortRows" :key="row.cohortLabel" class="border-t border-surface-100">
                  <td class="py-1.5 pr-4 text-ink-600 font-medium">{{ row.cohortLabel }}</td>
                  <td class="py-1.5 px-1 text-center text-ink-400 font-mono">{{ row.cohortSize }}</td>
                  <td v-for="(val, i) in row.retentionRates" :key="i" class="py-1.5 px-1 text-center">
                    <span class="inline-block w-10 py-0.5 rounded text-xs font-semibold" :style="heatStyle(val)">
                      {{ val != null ? val + '%' : '—' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, watch } from 'vue'
import ContentHeader from '../components/ContentHeader.vue'
import StatCard from '../components/StatCard.vue'
import ReportChart from '../components/ReportChart.vue'
import { COLORS, makeLineDataset } from '../composables/useChartData.js'
import { useAuthStore, useMetricsStore, useProjectStore } from '../stores/index'
import { useGamificationStore } from '../gamification/stores/gamification'

const store = useMetricsStore()
const projectStore = useProjectStore()
const loading = computed(() => store.loading.retention)

const gamification = useGamificationStore()
const auth = useAuthStore()

const DAY_LABELS = ['D1','D2','D3','D4','D5','D6','D7','D14','D21','D30']

function getRate(type, day) {
  const arr = type === 'classic' ? store.retention?.classic : store.retention?.rolling
  const v = arr?.find(r => r.day === day)?.rate
  return v != null ? v : '—'
}

const cohortRows = computed(() => store.retention?.cohortTable ?? [])

const classicChart = computed(() => {
  const pts = store.retention?.classic ?? []
  const vals = DAY_LABELS.map(l => pts.find(p => p.day === +l.replace('D',''))?.rate ?? null)
  return { labels: DAY_LABELS, datasets: [makeLineDataset('Retention %', vals, COLORS.blue, COLORS.blueFade)] }
})

const rollingChart = computed(() => {
  const pts = store.retention?.rolling ?? []
  const vals = DAY_LABELS.map(l => pts.find(p => p.day === +l.replace('D',''))?.rate ?? null)
  return { labels: DAY_LABELS, datasets: [makeLineDataset('Rolling %', vals, COLORS.blue, COLORS.blueFade)] }
})

const kpis = computed(() => [
  { label: 'D1 Retention',  value: getRate('classic', 1)  + '%' },
  { label: 'D7 Retention',  value: getRate('classic', 7)  + '%' },
  { label: 'D14 Retention', value: getRate('classic', 14) + '%' },
  { label: 'D30 Retention', value: getRate('classic', 30) + '%' },
])

function heatStyle(val) {
  if (val == null) return { background: '#f0f2f7', color: '#9aa2b8' }
  const t = val / 100
  return { background: `rgba(43,127,255,${0.08 + t * 0.82})`, color: t > 0.4 ? '#1040b6' : '#2b7fff' }
}


async function reload() {
  const pid = projectStore.activeProjectId
  //await gamification.emitEvent(auth.userId, 'dashview', 1)
  //await gamification.checkNewAchievementsAfterEvent(auth.userId)
  if (pid) { const { from, to } = store.dateRange; await store.fetchRetention(pid, from, to) }
}
onMounted(reload)
watch(() => projectStore.activeProjectId, reload)
watch(() => store.dateRange, reload, { deep: true })
</script>
