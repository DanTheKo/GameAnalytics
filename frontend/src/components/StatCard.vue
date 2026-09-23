<template>
  <div class="stat-card">
    <div class="flex items-start justify-between mb-3">
      <div>
        <p class="text-xs font-medium text-ink-400 mb-1">{{ label }}</p>
        <p class="text-2xl font-bold text-ink-900 tracking-tight">{{ value }}</p>
      </div>
      <div v-if="info" class="relative">
        <button
          class="text-ink-300 hover:text-ink-500 transition-colors mt-0.5"
          @mouseenter="open = true"
          @mouseleave="open = false"
        >
          <Info :size="14" />
        </button>
        <div v-if="open"
          class="absolute right-0 top-full mt-1 w-52 bg-ink-900 text-white text-xs rounded-lg p-3 z-50 shadow-float leading-relaxed pointer-events-none">
          {{ info }}
        </div>
      </div>
    </div>

    <div v-if="delta != null" class="flex items-center gap-1.5 mb-3">
      <span
        class="inline-flex items-center gap-0.5 text-xs font-semibold px-1.5 py-0.5 rounded"
        :class="delta >= 0 ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600'"
      >
        <component :is="delta >= 0 ? TrendingUp : TrendingDown" :size="11" />
        {{ Math.abs(delta) }}%
      </span>
      <span class="text-xs text-ink-300">vs last period</span>
    </div>

    <slot />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { Info, TrendingUp, TrendingDown } from 'lucide-vue-next'

defineProps({
  label: String,
  value: [String, Number],
  delta: Number,
  info:  String,
})

const open = ref(false)
</script>
