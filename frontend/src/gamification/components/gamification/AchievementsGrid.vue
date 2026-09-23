<script setup lang="ts">
import { ref, computed } from 'vue'
import type { AchievementWithStatus } from '../../api/types'
import AchievementBadge from './AchievementBadge.vue'
import { Filter, Search, Trophy, Star, Lock } from 'lucide-vue-next'

const props = defineProps<{
  achievements: AchievementWithStatus[]
  isLoading?: boolean
}>()

const emit = defineEmits<{
  (e: 'select', achievement: AchievementWithStatus): void
}>()

// Filters
const searchQuery = ref('')
const filterStatus = ref<'all' | 'unlocked' | 'locked'>('all')
const sortBy = ref<'name' | 'progress' | 'points'>('name')

// Filtered & sorted achievements
const filteredAchievements = computed(() => {
  let result = [...props.achievements]
  
  // Search filter
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(a => 
      a.name.toLowerCase().includes(query) || 
      a.description.toLowerCase().includes(query)
    )
  }
  
  // Status filter
  if (filterStatus.value !== 'all') {
    result = result.filter(a => 
      filterStatus.value === 'unlocked' ? a.isUnlocked : !a.isUnlocked
    )
  }
  
  // Sorting
  result.sort((a, b) => {
    if (sortBy.value === 'name') return a.name.localeCompare(b.name)
    if (sortBy.value === 'progress') return b.percentage - a.percentage
    if (sortBy.value === 'points') return (b.points || 0) - (a.points || 0)
    return 0
  })
  
  return result
})

// Stats
const stats = computed(() => ({
  total: props.achievements.length,
  unlocked: props.achievements.filter(a => a.isUnlocked).length,
  locked: props.achievements.filter(a => !a.isUnlocked).length,
  totalPoints: props.achievements
    .filter(a => a.isUnlocked)
    .reduce((sum, a) => sum + (a.points || 0), 0)
}))
</script>

<template>
  <div class="space-y-4">
    <!-- Stats Bar -->
    <div class="flex items-center gap-4 p-3 bg-surface-50 rounded-lg border border-surface-100">
      <div class="flex items-center gap-1.5">
        <Trophy :size="14" class="text-amber-500" />
        <span class="font-semibold text-ink-900">{{ stats.unlocked }}</span>
        <span class="font-semibold text-ink-900">/ {{ stats.total }}</span>
      </div>
      <div class="w-px h-4 bg-surface-200"></div>
      <div class="flex items-center gap-1">
        <!-- <Star :size="12" class="text-amber-400" fill="currentColor" />
        <span class="text-xs font-semibold text-ink-900">{{ stats.totalPoints }}</span>
        <span class="text-[11px] text-ink-400">XP earned</span>-->
      </div>
      <div class="flex-1"></div>
      <div class="flex items-center gap-1 text-[15px] text-ink-400">
        <Lock :size="12" />
        {{ stats.locked }} locked
      </div>
    </div>

    <!-- Filters -->
    <div class="flex flex-wrap items-center gap-2">
      <!-- Search -->
      <div class="relative flex-1 min-w-[200px]">
        <Search :size="14" class="absolute left-3 top-1/2 -translate-y-1/2 text-ink-300" />
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search achievements..."
          class="w-full pl-9 pr-3 py-2 text-sm bg-surface-0 border border-surface-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-400 transition-colors"
        />
      </div>

      <!-- Status Filter -->
      <div class="flex items-center bg-surface-100 rounded-lg p-0.5">
        <button
          v-for="opt in ['all', 'unlocked', 'locked'] as const"
          :key="opt"
          @click="filterStatus = opt"
          class="px-3 py-1.5 text-sm font-medium rounded-md transition-all capitalize"
          :class="filterStatus === opt 
            ? 'bg-surface-0 text-ink-900 shadow-sm' 
            : 'text-ink-400 hover:text-ink-600'"
        >
          {{ opt }}
        </button>
      </div>

      <!-- Sort -->
      <div class="flex items-center gap-1.5">
        <Filter :size="14" class="text-ink-400" />
        <select
          v-model="sortBy"
          class="px-2.5 py-1.5 text-[15px] bg-surface-0 border border-surface-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500/20"
        >
          <option value="name">Name</option>
          <option value="progress">Progress</option>
          <option value="points">XP Value</option>
        </select>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="py-8 flex justify-center">
      <div class="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="filteredAchievements.length === 0" class="py-12 text-center">
      <div class="inline-flex items-center justify-center w-12 h-12 rounded-full bg-surface-100 mb-3">
        <Star :size="20" class="text-ink-300" />
      </div>
      <p class="text-sm text-ink-500">No achievements match your filters</p>
      <button 
        @click="{ searchQuery = ''; filterStatus = 'all' }"
        class="mt-2 text-brand-500 hover:text-brand-600"
      >
        Clear filters
      </button>
    </div>

    <!-- Achievements Grid -->
    <div v-else class="grid grid-cols-1 gap-3">
      <AchievementBadge
        v-for="ach in filteredAchievements"
        :key="ach.id"
        :achievement="ach"
        :compact="false"
        show-progress
        @click="emit('select', ach)"
      />
    </div>
  </div>
</template>