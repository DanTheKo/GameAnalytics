<script setup lang="ts">
import type { AchievementWithStatus } from '../../api/types'
import { CheckCircle2, Lock, Trophy } from 'lucide-vue-next'

defineProps<{
  achievement: AchievementWithStatus
  showProgress?: boolean
  compact?: boolean
}>()

const emit = defineEmits<{
  (e: 'click', achievement: AchievementWithStatus): void
}>()
</script>

<template>
  <button
    @click="emit('click', achievement)"
    class="w-full text-left group"
    :class="[
      compact ? 'p-2' : 'p-4',
      achievement.isUnlocked 
        ? 'bg-surface-0 border-surface-200 hover:border-brand-300' 
        : 'bg-surface-50 border-surface-100 opacity-80 hover:opacity-100',
      'rounded-lg border transition-all duration-200'
    ]"
  >
  
    <div class="flex items-start gap-3">
      <!-- Icon -->
      <div 
        class="relative shrink-0 rounded-full flex items-center justify-center transition-colors"
        :class="[
          compact ? 'w-8 h-8' : 'w-12 h-12',
          achievement.isUnlocked 
            ? 'bg-brand-50 text-brand-500' 
            : 'bg-surface-100 text-ink-300'
        ]"
      >
        <component 
          :is="achievement.icon || 'Star'" 
          :size="compact ? 14 : 18" 
        />
        <!--<CheckCircle2 
          v-if="achievement.isUnlocked" 
          :size="10" 
          class="absolute -bottom-0.5 -right-0.5 text-brand-500 bg-surface-0 rounded-full border border-surface-0"
        />-->
      </div>

      <!-- Content -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2">
          <h4 
            class="font-semibold text-ink-900 truncate"
            :class="compact ? 'text-sm' : 'text-base'"
          >
            {{ achievement.name }}
          </h4>
          <Trophy 
            v-if="achievement.points && achievement.points >= 0 && achievement.isUnlocked" 
            :size="12" 
            class="text-amber-500"
          />
        </div>
        
        <p 
          class="text-ink-400 truncate mt-0.5"
          :class="compact ? 'text-[14px]' : 'text-s'"
        >
          {{ achievement.description }}
        </p>

        <!-- Progress Bar -->
        <div v-if="showProgress && !achievement.isUnlocked" class="mt-2">
          <div class="flex justify-between text-[14px] text-ink-400 mb-1">
            <span>{{ achievement.progress }} / {{ achievement.targetValue }}</span>
            <span>{{ achievement.percentage }}%</span>
          </div>
          <div class="h-1.5 w-full bg-surface-100 rounded-full overflow-hidden">
            <div 
              class="h-full bg-brand-400 rounded-full transition-all duration-500"
              :style="{ width: `${achievement.percentage}%` }"
            ></div>
          </div>
        </div>

        <!-- Unlocked Date -->
        <p 
          v-if="achievement.isUnlocked && achievement.achievedAt" 
          class="text-[14px] text-ink-300 mt-1 flex items-center gap-1"
        >
          <CheckCircle2 :size="10" />
          Unlocked {{ new Date(achievement.achievedAt).toLocaleDateString() }}
        </p>

        <!-- Points Badge -->
        <span 
          v-if="achievement.points" 
          class="inline-flex items-center gap-1 mt-2 px-2 py-0.5 rounded-full text-[14px] font-medium"
          :class="achievement.isUnlocked 
            ? 'bg-amber-50 text-amber-700' 
            : 'bg-surface-100 text-ink-400'"
        >
          <Trophy :size="10" />
          +{{ achievement.points }} XP
        </span>
      </div>
    </div>
  </button>
</template>