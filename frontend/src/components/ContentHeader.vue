<template>
  <div class="px-6 pt-5 pb-4 bg-white border-b border-surface-200">
    <div class="flex items-start justify-between mb-3">
      <div>
        <h1 class="text-lg font-bold text-ink-900">{{ title }}</h1>
        <p class="text-xs text-ink-300 mt-0.5 font-mono">Updated: {{ updatedAt }}</p>
      </div>
    </div>

    <div class="flex items-center gap-2 flex-wrap">
      <div class="relative">
        <button class="chip" @click="pickerOpen = !pickerOpen">
          <Calendar :size="12" />
          <span class="font-mono text-[11px]">{{ rangeLabel }}</span>
          <ChevronDown :size="11" />
        </button>

        <div v-if="pickerOpen" class="absolute top-full left-0 mt-1 bg-white border border-surface-200 rounded-lg shadow-float z-50 p-3 w-64">
          <div class="flex gap-1 mb-3 bg-surface-100 rounded p-0.5">
            <button v-for="p in presets" :key="p.label"
              class="flex-1 py-1 text-[11px] font-medium rounded transition-all"
              :class="activePreset === p.label ? 'bg-white text-ink-900 shadow-sm' : 'text-ink-400 hover:text-ink-700'"
              @click="applyPreset(p)"
            >{{ p.label }}</button>
          </div>
          <div class="space-y-2">
            <div>
              <label class="block text-[10px] font-medium text-ink-400 mb-0.5">From</label>
              <input type="date" v-model="localFrom"
                class="w-full px-2 py-1.5 text-xs border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all" />
            </div>
            <div>
              <label class="block text-[10px] font-medium text-ink-400 mb-0.5">To</label>
              <input type="date" v-model="localTo"
                class="w-full px-2 py-1.5 text-xs border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all" />
            </div>
          </div>
          <button class="btn-primary text-xs w-full justify-center mt-3" @click="applyCustom">Apply</button>
        </div>
      </div>

      <div class="w-px h-4 bg-surface-200 mx-1"></div>

      <button v-for="filter in filters" :key="filter" class="chip">
        {{ filter }}
        <ChevronDown :size="11" />
      </button>
    </div>
  </div>

  <div v-if="pickerOpen" class="fixed inset-0 z-40" @click="pickerOpen = false" />
</template>

<script>
const _now = new Date()
const _fmt = (d) => d.toLocaleDateString('en-GB', { month: 'short', day: 'numeric', year: 'numeric' })
const _monthAgo = new Date(_now); _monthAgo.setDate(_now.getDate() - 30)
const _defaultUpdatedAt = _fmt(_now) + ', ' + _now.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })

export default {
  props: {
    title:     { type: String, default: 'Analytics' },
    updatedAt: { type: String, default: _defaultUpdatedAt },
    filters:   { type: Array,  default: () => ['Country', 'Platform', 'Version', 'Audience'] },
  },
  emits: ['dateChange'],
}
</script>

<script setup>
import { ref, computed } from 'vue'
import { Calendar, ChevronDown } from 'lucide-vue-next'
import { useMetricsStore } from '../stores/index'

const emit = defineEmits(['dateChange'])
const metricsStore = useMetricsStore()
const pickerOpen = ref(false)

const toDateInput = (iso) => iso.slice(0, 10)

const localFrom = ref(toDateInput(metricsStore.dateRange.from))
const localTo   = ref(toDateInput(metricsStore.dateRange.to))
const activePreset = ref('30D')

const presets = [
  { label: '7D',  days: 7  },
  { label: '30D', days: 30 },
  { label: '90D', days: 90 },
]

const rangeLabel = computed(() => {
  const fmt = (s) => new Date(s).toLocaleDateString('en-GB', { month: 'short', day: 'numeric' })
  return `${fmt(metricsStore.dateRange.from)} – ${fmt(metricsStore.dateRange.to)}`
})

function applyPreset(p) {
  activePreset.value = p.label
  const to   = new Date()
  const from = new Date(); from.setDate(from.getDate() - p.days)
  localFrom.value = toDateInput(from.toISOString())
  localTo.value   = toDateInput(to.toISOString())
  metricsStore.setDateRange(from.toISOString(), to.toISOString())
  pickerOpen.value = false
  emit('dateChange', { from: from.toISOString(), to: to.toISOString() })
}

function applyCustom() {
  activePreset.value = ''
  const from = new Date(localFrom.value).toISOString()
  const to   = new Date(localTo.value + 'T23:59:59').toISOString()
  metricsStore.setDateRange(from, to)
  pickerOpen.value = false
  emit('dateChange', { from, to })
}

</script>
