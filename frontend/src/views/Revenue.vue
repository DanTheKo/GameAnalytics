<template>
  <div>
    <ContentHeader title="Revenue" :filters="['Country', 'Platform', 'Version', 'Item Type']" />
    <div class="p-6 space-y-5">
      <div class="grid grid-cols-2 lg:grid-cols-5 gap-4">
        <StatCard v-for="(k, i) in kpis" :key="k.label" :label="k.label" :value="k.value" :delta="k.delta" :class="`fade-up fade-up-${i+1}`" />
      </div>

      <div v-if="loading" class="grid grid-cols-1 xl:grid-cols-1 gap-4">
        <div v-for="n in 2" :key="n" class="stat-card h-64 animate-pulse bg-surface-100 rounded-lg" />
      </div>

      <template v-else>
        <div class="grid grid-cols-1 xl:grid-cols-1 gap-4">
          <ReportChart class="fade-up fade-up-1" title="Daily Revenue"
          info="Total revenue from in-app purchases per day, from events with EventType = Revenue." subtitle="Total in-app purchase revenue per day"
            type="bar" :chart-data="dailyRevenueChart" :height="220"
            :kpis="[{ label:'Total', value: formatMoney(store.revenue?.totalRevenue ?? 0), color: COLORS.blue }]" />
          <!--<ReportChart class="fade-up fade-up-2" title="ARPU & ARPPU"
          info="ARPU = total revenue / all active users. ARPPU = total revenue / paying users only." subtitle="Average revenue per user / paying user"
            type="line" :chart-data="arpuChart" :height="220"
            :kpis="[
              { label:'ARPU',  value: '$' + (store.revenue?.arpu  ?? 0).toFixed(2), color: COLORS.blue },
              { label:'ARPPU', value: '$' + (store.revenue?.arppu ?? 0).toFixed(2), color: COLORS.rose },
            ]" />-->
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
import { COLORS, dayLabels, randomWalk, makeLineDataset, makeBarDataset } from '../composables/useChartData.js'
import { useMetricsStore, useProjectStore } from '../stores/index'

const store = useMetricsStore()
const projectStore = useProjectStore()
const loading = computed(() => store.loading.revenue)

function formatMoney(v) {
  if (!v) return '$0'
  return '$' + (v >= 1000 ? (v / 1000).toFixed(1) + 'K' : v.toFixed(0))
}

function toLabels(pts) {
  return (pts ?? []).map(p => new Date(p.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }))
}

const dailyRevenueChart = computed(() => {
  const pts = store.revenue?.dailyRevenue
  if (!pts?.length) return { labels: dayLabels(30), datasets: [makeBarDataset('Revenue', randomWalk(30,0,0,0), COLORS.blue)] }
  return { labels: toLabels(pts), datasets: [makeBarDataset('Revenue $', pts.map(p => p.value), COLORS.blue)] }
})

const arpuChart = computed(() => {
  // ARPU/ARPPU are scalar — show them as flat lines over the period for visual context
  const pts = store.revenue?.dailyRevenue ?? []
  const arpu  = store.revenue?.arpu  ?? 0
  const arppu = store.revenue?.arppu ?? 0
  const labels = pts.length ? toLabels(pts) : dayLabels(30)
  return {
    labels,
    datasets: [
      makeLineDataset('ARPU',  Array(labels.length).fill(arpu),  COLORS.blue, COLORS.blueFade),
      makeLineDataset('ARPPU', Array(labels.length).fill(arppu), COLORS.rose, 'rgba(244,63,94,0.10)'),
    ],
  }
})

const kpis = computed(() => [
  { label: 'Revenue',   value: formatMoney(store.revenue?.totalRevenue ?? 0), info: 'Total in-app purchase revenue for the selected period.' },
  { label: 'Paying Users',  value: String(store.revenue?.payingUsers ?? '—'), info: 'Unique users who made at least one purchase.' },
  { label: 'Conv. Rate',    value: (store.revenue?.payerConversionRate ?? 0).toFixed(2) + '%', info: 'Percentage of active users who made a purchase.' },
  { label: 'ARPU',         value: '$' + (store.revenue?.arpu ?? 0).toFixed(2), info: 'Average revenue per user for the selected period.' },
  { label: 'ARPPU',         value: '$' + (store.revenue?.arppu ?? 0).toFixed(2), info: 'Average revenue per paying user for the selected period.' },
])

async function reload() {
  const pid = projectStore.activeProjectId
  if (pid) { const { from, to } = store.dateRange; await store.fetchRevenue(pid, from, to) }
}
onMounted(reload)
watch(() => projectStore.activeProjectId, reload)
watch(() => store.dateRange, reload, { deep: true })
</script>
