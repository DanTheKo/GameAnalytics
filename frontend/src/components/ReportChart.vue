<template>
  <div class="stat-card">
    <div class="flex items-center justify-between mb-4">
      <div>
        <h3 class="text-sm font-semibold text-ink-900">{{ title }}</h3>
        <p v-if="subtitle" class="text-xs text-ink-400 mt-0.5">{{ subtitle }}</p>
      </div>
      <div class="flex items-center gap-1">
        <!-- Info tooltip -->
        <div v-if="info" class="relative">
          <button
            class="text-ink-300 hover:text-ink-500 transition-colors"
            @mouseenter="infoOpen = true"
            @mouseleave="infoOpen = false"
          >
            <Info :size="14" />
          </button>
          <div v-if="infoOpen"
            class="absolute right-0 top-full mt-1 w-56 bg-ink-900 text-white text-xs rounded-lg p-3 z-50 shadow-float leading-relaxed pointer-events-none">
            {{ info }}
          </div>
        </div>

        <!-- More menu -->
        <div class="relative">
          <button class="text-ink-300 hover:text-ink-500 transition-colors" @click="menuOpen = !menuOpen">
            <MoreHorizontal :size="14" />
          </button>
          <div v-if="menuOpen"
            class="absolute right-0 top-full mt-1 w-44 bg-white border border-surface-200 rounded-lg shadow-float z-50 py-1">
            <button class="w-full flex items-center gap-2 px-3 py-2 text-xs text-ink-700 hover:bg-surface-50 transition-colors" @click="downloadCsv">
              <Download :size="13" /> Download CSV
            </button>
            <button class="w-full flex items-center gap-2 px-3 py-2 text-xs text-ink-700 hover:bg-surface-50 transition-colors" @click="viewTable = !viewTable">
              <Table2 :size="13" /> {{ viewTable ? 'View chart' : 'View as table' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div v-if="kpis?.length" class="flex items-center gap-3 mb-4">
      <div v-for="kpi in kpis" :key="kpi.label" class="flex items-center gap-2">
        <span class="w-2 h-2 rounded-full" :style="{ background: kpi.color }"></span>
        <div>
          <span class="text-[11px] text-ink-400">{{ kpi.label }}: </span>
          <span class="text-[11px] font-semibold text-ink-900">{{ kpi.value }}</span>
        </div>
      </div>
    </div>

    <!-- Table view -->
    <div v-if="viewTable && chartData" class="overflow-x-auto max-h-48 overflow-y-auto">
      <table class="w-full text-xs">
        <thead class="sticky top-0 bg-white">
          <tr class="border-b border-surface-100">
            <th v-for="col in tableColumns" :key="col" class="text-left text-ink-400 font-semibold pb-2 pr-4">{{ col }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, i) in tableRows" :key="i" class="border-b border-surface-50">
            <td v-for="col in tableColumns" :key="col" class="py-1.5 pr-4 text-ink-700">{{ row[col] }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Chart view -->
    <div v-else class="relative" :style="{ height: height + 'px' }">
      <component :is="chartComponent" v-if="chartData" :data="chartData" :options="mergedOptions" />
      <div v-else class="absolute inset-0 flex items-center justify-center">
        <div class="flex flex-col items-center gap-2">
          <BarChart2 :size="28" class="text-ink-100" />
          <p class="text-xs text-ink-300">No data for this period</p>
        </div>
      </div>
    </div>
  </div>

  <div v-if="menuOpen" class="fixed inset-0 z-40" @click="menuOpen = false" />
</template>

<script setup>
import { ref, computed } from 'vue'
import { Line, Bar, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS, CategoryScale, LinearScale, PointElement,
  LineElement, BarElement, ArcElement, Tooltip, Legend, Filler
} from 'chart.js'
import { Info, MoreHorizontal, BarChart2, Download, Table2 } from 'lucide-vue-next'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, BarElement, ArcElement, Tooltip, Legend, Filler)

const props = defineProps({
  title:     String,
  subtitle:  String,
  info:      String,
  type:      { type: String, default: 'line' },
  chartData: Object,
  options:   Object,
  height:    { type: Number, default: 200 },
  periods:   Array,
  kpis:      Array,
})

const infoOpen = ref(false)
const menuOpen = ref(false)
const viewTable = ref(false)

const chartComponent = computed(() => ({ line: Line, bar: Bar, doughnut: Doughnut }[props.type] ?? Line))

const tableColumns = computed(() => {
  if (!props.chartData) return []
  return ['label', ...props.chartData.datasets.map(d => d.label ?? 'value')]
})

const tableRows = computed(() => {
  if (!props.chartData?.labels) return []
  return props.chartData.labels.map((label, i) => {
    const row = { label }
    props.chartData.datasets.forEach(ds => {
      row[ds.label ?? 'value'] = ds.data[i] ?? '—'
    })
    return row
  })
})

function downloadCsv() {
  if (!props.chartData?.labels) return
  const cols = tableColumns.value
  const rows = tableRows.value
  const csv = [cols.join(','), ...rows.map(r => cols.map(c => r[c]).join(','))].join('\n')
  const a = document.createElement('a')
  a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv' }))
  a.download = (props.title ?? 'chart') + '.csv'
  a.click()
  menuOpen.value = false
}

const defaultOptions = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: '#0f1523',
      titleColor: '#9aa2b8',
      bodyColor: '#ffffff',
      padding: 10,
      cornerRadius: 8,
      titleFont: { family: 'DM Sans', size: 11 },
      bodyFont: { family: 'DM Sans', size: 12, weight: '600' },
    },
  },
  scales: {
    x: {
      border: { display: false },
      grid: { display: false },
      ticks: { color: '#9aa2b8', font: { family: 'DM Sans', size: 11 }, maxTicksLimit: 6 },
    },
    y: {
      border: { display: false },
      grid: { color: '#f0f2f7', lineWidth: 1 },
      ticks: { color: '#9aa2b8', font: { family: 'DM Sans', size: 11 }, maxTicksLimit: 5 },
      beginAtZero: true,
    },
  },
}

const mergedOptions = computed(() => deepMerge(defaultOptions, props.options ?? {}))

function deepMerge(a, b) {
  const out = { ...a }
  for (const k in b) {
    if (b[k] && typeof b[k] === 'object' && !Array.isArray(b[k])) {
      out[k] = deepMerge(a[k] ?? {}, b[k])
    } else {
      out[k] = b[k]
    }
  }
  return out
}
</script>
