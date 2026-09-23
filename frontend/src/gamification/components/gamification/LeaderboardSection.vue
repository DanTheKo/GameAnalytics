<script setup lang="ts">
import { ref, computed } from 'vue'
import { LeaderboardEntry } from '../../stores/gamification'
import { Trophy, Medal, Award, ChevronRight, Crown } from 'lucide-vue-next'


const props = defineProps<{
  entries: LeaderboardEntry[]
  currentUserId?: string
  isLoading?: boolean
  metric?: 'xp' | 'level' | 'achievements'
}>()


const emit = defineEmits<{
  (e: 'view-profile', userId: string): void
  (e: 'update:metric', metric: 'xp' | 'level' | 'achievements'): void
}>()

// Metric labels
const metricLabels: Record<string, string> = {
  xp: 'Total XP',
  level: 'Level',
  achievements: 'Achievements',
}

// Get rank icon/color
function getRankStyle(rank: number) {
  if (rank === 1) return { icon: Crown, color: 'text-amber-500', bg: 'bg-amber-50', border: 'border-amber-200' }
  if (rank === 2) return { icon: Medal, color: 'text-slate-400', bg: 'bg-slate-50', border: 'border-slate-200' }
  if (rank === 3) return { icon: Award, color: 'text-amber-700', bg: 'bg-amber-50', border: 'border-amber-200' }
  return { icon: null, color: 'text-ink-400', bg: 'bg-surface-50', border: 'border-surface-100' }
}

// Check if entry is current user
function isCurrentUser(entry: LeaderboardEntry) {
  return props.currentUserId && entry.userId === props.currentUserId
}
</script>

<template>
  <div class="space-y-4">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-sm font-semibold text-ink-900">Leaderboard</h3>
        <p class="text-ink-400 mt-0.5">Top players by {{ metricLabels[metric || 'xp'] }}</p>
      </div>
      <div class="flex items-center gap-1 bg-surface-100 rounded-lg p-0.5">
        <button
          v-for="m in ['xp', 'level', 'achievements'] as const"
          :key="m"
          @click="$emit('update:metric', m)"
          class="px-2.5 py-1 text-sm font-medium rounded transition-all capitalize"
          :class="$props.metric === m 
            ? 'bg-surface-0 text-ink-900 shadow-sm' 
            : 'text-ink-400 hover:text-ink-600'"
        >
          {{ m }}
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="py-8 flex justify-center">
      <div class="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Leaderboard List -->
    <div v-else class="space-y-2">
      <div
        v-for="entry in entries"
        :key="entry.userId"
        @click="emit('view-profile', entry.userId)"
        class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-all hover:bg-surface-50 hover:border-surface-300 group"
        :class="[
          isCurrentUser(entry) 
            ? 'bg-brand-50/50 border-brand-200 ring-1 ring-brand-300/30' 
            : 'bg-surface-0 border-surface-100',
          getRankStyle(entry.rank).bg
        ]"
      >
        <!-- Rank -->
        <div 
          class="w-8 h-8 rounded-full flex items-center justify-center shrink-0 font-bold text-sm"
          :class="[
            getRankStyle(entry.rank).color,
            entry.rank <= 3 ? 'ring-2 ring-offset-2 ' + getRankStyle(entry.rank).border : ''
          ]"
        >
          <component :is="getRankStyle(entry.rank).icon" v-if="entry.rank <= 3" :size="16" />
          <span v-else>#{{ entry.rank }}</span>
        </div>

        <!-- Avatar + Name -->
        <div class="flex items-center gap-3 flex-1 min-w-0">
          <img
            :src="entry.avatarUrl"
            :alt="entry.name"
            class="w-10 h-10 rounded-full object-cover border border-surface-200"
          />
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <span 
                class="text-sm font-semibold text-ink-900 truncate"
                :class="isCurrentUser(entry) ? 'text-brand-700' : ''"
              >
                {{ entry.name }}
              </span>
              <span 
                v-if="isCurrentUser(entry)" 
                class="px-1.5 py-0.5 bg-brand-500 text-white text-[15px] font-medium rounded"
              >
                You
              </span>
            </div>
            <!--<span class="text-[11px] text-ink-400">
              {{ metric === 'level' ? 'Level' : metric === 'achievements' ? 'Badges' : 'XP' }}: 
              <span class="font-semibold text-ink-700">{{ entry.value.toLocaleString() }}</span>
            </span>-->
          </div>
        </div>

        <!-- Value + Arrow -->
        <div class="flex items-center gap-2">
          <span 
            class="text-sm font-bold text-ink-900"
            :class="entry.rank <= 3 ? 'text-brand-600' : ''"
          >
            {{ entry.value.toLocaleString() }}
          </span>
          <ChevronRight :size="16" class="text-ink-300 group-hover:text-ink-500 transition-colors" />
        </div>
      </div>
    </div>

    <!-- View All Button 
    <button class="w-full py-2.5 text-sm font-medium text-brand-500 hover:text-brand-600 border border-dashed border-brand-300 rounded-lg hover:bg-brand-50/50 transition-colors flex items-center justify-center gap-2">
      View Full Leaderboard
      <ChevronRight :size="14" />
    </button>-->
  </div>
</template>