<template>
  <div>
    <ContentHeader title="Funnels" :filters="['Country', 'Platform', 'Version', 'Date Range']" />

    <div class="p-6 space-y-5">
      <!-- Funnel selector -->
      <div class="flex items-center gap-2 flex-wrap">
        <button
          v-for="f in funnelOptions"
          :key="f"
          class="chip"
          :class="{ active: selectedFunnel === f }"
          @click="selectedFunnel = f"
        >{{ f }}</button>
        <button class="btn-ghost text-xs ml-auto">+ New Funnel</button>
      </div>

      <!-- Main funnel chart -->
      <div class="grid grid-cols-1 xl:grid-cols-3 gap-4">
        <!-- Funnel viz -->
        <div class="stat-card xl:col-span-1 fade-up fade-up-1">
          <h3 class="text-sm font-semibold text-ink-900 mb-1">{{ selectedFunnel }}</h3>
          <p class="text-xs text-ink-400 mb-5">Overall conversion: <span class="font-semibold text-ink-900">{{ overallConv }}%</span></p>

          <div class="space-y-2">
            <div v-for="(step, i) in funnelSteps" :key="step.name">
              <!-- Step bar -->
              <div class="flex items-center gap-3 mb-1">
                <span class="text-[11px] font-medium text-ink-600 w-28 shrink-0 truncate">{{ step.name }}</span>
                <div class="flex-1 h-8 bg-surface-100 rounded-lg overflow-hidden relative">
                  <div
                    class="h-full rounded-lg flex items-center px-3 transition-all duration-500"
                    :style="{ width: step.pct + '%', background: stepColor(i) }"
                  >
                    <span class="text-xs font-bold text-white drop-shadow">{{ step.count.toLocaleString() }}</span>
                  </div>
                </div>
                <span class="text-[11px] font-semibold text-ink-900 w-10 text-right shrink-0">{{ step.pct }}%</span>
              </div>
              <!-- Drop arrow -->
              <div v-if="i < funnelSteps.length - 1" class="flex items-center gap-3 mb-1">
                <span class="w-28 shrink-0"></span>
                <div class="flex-1 flex items-center gap-1.5 pl-2">
                  <span class="text-red-400 text-xs">↓</span>
                  <span class="text-[10px] text-red-400 font-medium">
                    −{{ (funnelSteps[i].count - funnelSteps[i + 1].count).toLocaleString() }} dropped
                    ({{ (100 - Math.round(funnelSteps[i + 1].count / funnelSteps[i].count * 100)) }}%)
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Conversion rate over time -->
        <div class="xl:col-span-2 space-y-4">
          <ReportChart
            class="fade-up fade-up-2"
            title="Funnel Conversion Over Time"
            subtitle="Daily overall conversion rate"
            type="line"
            :chart-data="convOverTime"
            :height="180"
            :periods="['7D', '30D']"
            :kpis="[{ label: 'Avg Conv.', value: overallConv + '%', color: COLORS.blue }]"
          />
          <ReportChart
            class="fade-up fade-up-3"
            title="Step Drop-off by Day"
            type="bar"
            :chart-data="dropoff"
            :height="180"
            :kpis="[
              { label: 'Biggest drop', value: 'Tutorial → Lobby', color: COLORS.rose },
            ]"
            :options="{ scales: { x: { stacked: true }, y: { stacked: true } } }"
          />
        </div>
      </div>

      <!-- Step-by-step table -->
      <div class="stat-card fade-up fade-up-4">
        <h3 class="text-sm font-semibold text-ink-900 mb-4">Step Analysis</h3>
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-surface-100">
              <th class="text-left text-xs font-semibold text-ink-400 pb-2 pr-4">#</th>
              <th class="text-left text-xs font-semibold text-ink-400 pb-2 pr-4">Step</th>
              <th class="text-right text-xs font-semibold text-ink-400 pb-2 pr-4">Users</th>
              <th class="text-right text-xs font-semibold text-ink-400 pb-2 pr-4">Conv. from prev</th>
              <th class="text-right text-xs font-semibold text-ink-400 pb-2 pr-4">Conv. from start</th>
              <th class="text-right text-xs font-semibold text-ink-400 pb-2">Avg Time</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(step, i) in funnelSteps" :key="step.name" class="border-b border-surface-50 hover:bg-surface-50 transition-colors">
              <td class="py-2.5 pr-4 text-ink-400 font-mono text-xs">{{ i + 1 }}</td>
              <td class="py-2.5 pr-4 font-medium text-ink-900">{{ step.name }}</td>
              <td class="py-2.5 pr-4 text-right font-mono text-xs text-ink-700">{{ step.count.toLocaleString() }}</td>
              <td class="py-2.5 pr-4 text-right">
                <span v-if="i === 0" class="text-xs text-ink-400">—</span>
                <span v-else class="text-xs font-semibold" :class="convFromPrev(i) >= 70 ? 'text-emerald-600' : convFromPrev(i) >= 50 ? 'text-amber-600' : 'text-red-500'">
                  {{ convFromPrev(i) }}%
                </span>
              </td>
              <td class="py-2.5 pr-4 text-right">
                <div class="flex items-center justify-end gap-2">
                  <div class="w-16 h-1.5 bg-surface-100 rounded-full overflow-hidden">
                    <div class="h-full rounded-full bg-brand-500" :style="{ width: step.pct + '%' }"></div>
                  </div>
                  <span class="text-xs font-semibold text-ink-900 w-8">{{ step.pct }}%</span>
                </div>
              </td>
              <td class="py-2.5 text-right text-xs text-ink-500 font-mono">{{ step.avgTime }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import ContentHeader from '../components/ContentHeader.vue'
import ReportChart from '../components/ReportChart.vue'
import { COLORS, dayLabels, randomWalk, makeLineDataset, makeBarDataset } from '../composables/useChartData.js'

const funnelOptions = ['Onboarding Flow', 'Purchase Flow', 'PvP Match Flow', 'Level Completion']
const selectedFunnel = ref('Onboarding Flow')

const funnelSteps = [
  { name: 'App Open',      count: 12481, pct: 100, avgTime: '0s'    },
  { name: 'Tutorial Start',count: 9840,  pct: 79,  avgTime: '12s'   },
  { name: 'Tutorial End',  count: 6210,  pct: 50,  avgTime: '4m 20s'},
  { name: 'Enter Lobby',   count: 4820,  pct: 39,  avgTime: '6m 10s'},
  { name: 'First Match',   count: 3940,  pct: 32,  avgTime: '7m 45s'},
  { name: 'Match Complete',count: 3210,  pct: 26,  avgTime: '14m 2s'},
]

const overallConv = computed(() => funnelSteps[funnelSteps.length - 1].pct)

const convFromPrev = (i) =>
  Math.round(funnelSteps[i].count / funnelSteps[i - 1].count * 100)

function stepColor(i) {
  const palette = [COLORS.blue, '#4d96ff', '#70aaff', '#93beff', '#b6d2ff', '#d9e9ff']
  return palette[i] ?? COLORS.blue
}

const labels = dayLabels(30)

const convOverTime = {
  labels,
  datasets: [makeLineDataset('Conv %', randomWalk(30, 26, 0.08, 10), COLORS.blue, COLORS.blueFade)],
}

const dropoff = {
  labels: labels.filter((_, i) => i % 5 === 0),
  datasets: [
    makeBarDataset('Tutorial drop', randomWalk(6, 600, 0.15, 0), COLORS.rose),
    makeBarDataset('Lobby drop',    randomWalk(6, 300, 0.15, 0), COLORS.amber),
    makeBarDataset('Match drop',    randomWalk(6, 150, 0.15, 0), COLORS.violet),
  ],
}
</script>
