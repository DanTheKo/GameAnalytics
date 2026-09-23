<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useGamificationStore } from '../../stores/gamification'
import type { AchievementWithStatus } from '../../api/types'
import { 
  X, Zap, Trophy, Star, Lock, CheckCircle2, 
  BarChart3, Users, Award, ChevronRight 
} from 'lucide-vue-next'
import AchievementsGrid from './AchievementsGrid.vue'
import LeaderboardSection from './LeaderboardSection.vue'
import {useAuthStore} from '../../../stores/index'

const store = useGamificationStore()
const authStore = useAuthStore()
const isOpen = defineModel<boolean>('modelValue', { required: true })

const username = authStore.username?.toString() ?? 'unknown_user';
const userId = authStore.userId?.toString() ?? 'unknown_id'; 

type TabType = 'overview' | 'achievements' | 'leaderboard'
const activeTab = ref<TabType>('overview')
const leaderboardMetric = ref<'xp' | 'level' | 'achievements'>('xp')


const selectedAchievement = ref<AchievementWithStatus | null>(null)

onMounted(async () => {
    await store.loadProfile(userId, username)
    await store.checkNewAchievementsAfterEvent(userId)
    await store.loadLeaderboard('xp')

  if (activeTab.value === 'leaderboard') {
    await store.loadLeaderboard(leaderboardMetric.value)
  }
})

const close = () => {
  isOpen.value = false
  selectedAchievement.value = null
}


const handleAchievementSelect = (ach: AchievementWithStatus) => {
  selectedAchievement.value = ach
}

const handleMetricChange = (metric: 'xp' | 'level' | 'achievements') => {
  leaderboardMetric.value = metric
  store.loadLeaderboard(metric)
  
}


</script>

<template>
  <Transition name="overlay">
    <div v-if="isOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <!-- Backdrop -->
      <div 
        class="absolute inset-0 bg-ink-900/40 backdrop-blur-sm transition-opacity" 
        @click="close"
      ></div>

      <!-- Modal Card -->
      <div class="h-screen relative w-full max-w-lg bg-surface-0 rounded-xl shadow-card overflow-hidden flex flex-col max-h-[90vh]">
        
        <!-- Loading State -->
        <div v-if="store.isLoading && !store.profile" class="p-12 flex flex-col items-center justify-center">
          <div class="w-8 h-8 border-2 border-brand-500 border-t-transparent rounded-full animate-spin"></div>
          <p class="text-sm text-ink-400 mt-4">Loading profile...</p>
        </div>

        <!-- Error State -->
        <div v-else-if="store.error" class="p-12 flex flex-col items-center justify-center">
          <p class="text-sm text-red-500">{{ store.error }}</p>
          <button 
            @click="store.loadProfile(userId, username)"
            class="mt-4 text-sm text-brand-500 hover:text-brand-600"
          >
            Try again
          </button>
        </div>

        <!-- Content -->
        <template v-else-if="store.profile">
          <!-- Header -->
          <div class="p-5 pb-4 border-b border-surface-100">
            <div class="flex items-start justify-between">
              <div class="flex items-center gap-4">
                <div class="relative">
                  <img 
                    :src="store.user?.avatarUrl" 
                    alt="Avatar" 
                    class="w-14 h-14 rounded-full object-cover border-2 border-surface-100"
                  />
                  <div class="absolute -bottom-1 -right-1 bg-brand-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full border-2 border-surface-0">
                    Lvl {{ store.progress?.level }}
                  </div>
                </div>
                <div>
                  <h2 class="text-base font-semibold text-ink-900">{{ username }}</h2>
                  <p class="text-base text-ink-400 mt-0.5">Member since {{ new Date(store.user?.createdAt ?? 2000).getFullYear() }}</p>
                </div>
              </div>
              <button 
                @click="close"
                class="text-ink-300 hover:text-ink-500 transition-colors p-1.5 rounded-lg hover:bg-surface-50"
              >
                <X :size="18" />
              </button>
            </div>

            <!-- XP Progress -->
            <div class="mt-4">
              <div class="flex justify-between text-[15px] mb-1.5">
                <span class="font-medium text-ink-700 flex items-center gap-1">
                  <Zap :size="11" class="text-brand-500" fill="currentColor" />
                  {{ store.progress?.currentXp.toLocaleString() }} / {{ store.progress?.nextLevelXp.toLocaleString() }} XP
                </span>
                <span class="text-ink-400">{{ store.xpPercentage }}%</span>
              </div>
              <div class="h-2 w-full bg-surface-100 rounded-full overflow-hidden">
                <div 
                  class="h-full bg-brand-500 rounded-full transition-all duration-700 ease-out"
                  :style="{ width: `${store.xpPercentage}%` }"
                ></div>
              </div>
            </div>
          </div>

          <!-- Tabs Navigation -->
          <div class="flex items-center border-b border-surface-100 bg-surface-50/50">
            <button
              v-for="tab in [
                { id: 'overview', label: 'Overview', icon: BarChart3 },
                { id: 'achievements', label: 'Achievements', icon: Trophy },
                { id: 'leaderboard', label: 'Leaderboard', icon: Users }
              ] as const"
              :key="tab.id"
              @click="activeTab = tab.id"
              class="flex-1 flex items-center justify-center gap-1.5 py-3 text-sm font-medium transition-colors relative"
              :class="activeTab === tab.id 
                ? 'text-brand-600' 
                : 'text-ink-400 hover:text-ink-600'"
            >
              <component :is="tab.icon" :size="14" />
              {{ tab.label }}
              <div 
                v-if="activeTab === tab.id" 
                class="absolute bottom-0 left-0 right-0 h-0.5 bg-brand-500 rounded-t"
              ></div>
            </button>
          </div>

          <!-- Tab Content -->
          <div class="overflow-y-auto p-5">
            
            <!-- Overview Tab -->
            <div v-if="activeTab === 'overview'" class="space-y-5">
              <!-- Stats Grid -->
              <div class="grid grid-cols-2 gap-3">
                <div class="bg-surface-50 rounded-lg p-3.5 border border-surface-100">
                  <div class="flex items-center gap-2 mb-2">
                    <BarChart3 :size="14" class="text-ink-400" />
                    <span class="text-[15px] font-semibold text-ink-500 uppercase tracking-wide">Events</span>
                  </div>
                  <div class="text-xl font-bold text-ink-900">
                    {{ store.progress?.totalEvents.toLocaleString() }}
                  </div>
                  <div class="text-[15px] text-ink-400 mt-0.5">Total actions</div>
                </div>
                <div class="bg-surface-50 rounded-lg p-3.5 border border-surface-100">
                  <div class="flex items-center gap-2 mb-2">
                    <Trophy :size="14" class="text-ink-400" />
                    <span class="text-[15px] font-semibold text-ink-500 uppercase tracking-wide">Unlocked</span>
                  </div>
                  <div class="text-xl font-bold text-ink-900">
                    {{ store.unlockedAchievementsCount }}/{{ store.achievements.length }}
                  </div>
                  <div class="text-[15px] text-ink-400 mt-0.5">Achievements</div>
                </div>
              </div>

              <!-- Recent Achievements Preview -->
              <div>
                <div class="flex items-center justify-between mb-3">
                  <h3 class="text-sm font-semibold text-ink-900">Recent Achievements</h3>
                  <button 
                    @click="activeTab = 'achievements'"
                    class="text-[15px] font-medium text-brand-500 hover:text-brand-600 flex items-center gap-0.5"
                  >
                    View All <ChevronRight :size="12" />
                  </button>
                </div>
                <div class="space-y-2">
                  <div 
                    v-for="ach in store.achievements.filter(a => a.isUnlocked).slice(0, 3)" 
                    :key="ach.id"
                    class="flex items-center gap-3 p-3 rounded-lg bg-surface-50 border border-surface-100"
                  >
                    <div class="w-9 h-9 rounded-full bg-brand-50 text-brand-500 flex items-center justify-center shrink-0">
                      <component :is="ach.icon || 'Star'" :size="16" />
                    </div>
                    <div class="flex-1 min-w-0">
                      <h4 class="text-sm font-semibold text-ink-900 truncate">{{ ach.name }}</h4>
                      <p class="text-[15px] text-ink-400 truncate">{{ ach.description }}</p>
                    </div>
                    <CheckCircle2 :size="14" class="text-brand-500 shrink-0" />
                  </div>
                </div>
              </div>

              <!-- Leaderboard Preview 
              <div>
                <div class="flex items-center justify-between mb-3">
                  <h3 class="text-sm font-semibold text-ink-900">Top Players</h3>
                  <button 
                    @click="activeTab = 'leaderboard'"
                    class="text-[11px] font-medium text-brand-500 hover:text-brand-600 flex items-center gap-0.5"
                  >
                    Full Board <ChevronRight :size="12" />
                  </button>
                </div>
                <LeaderboardSection
                  :entries="store.leaderboard.slice(0, 5)"
                  :current-user-id="USER_ID"
                  :is-loading="store.isLoading"
                  metric="xp"
                  @view-profile="(id) => console.log('View profile:', id)"
                />
              </div> -->
            </div>

            <!-- Achievements Tab -->
            <AchievementsGrid
              v-else-if="activeTab === 'achievements'"
              :achievements="store.achievements"
              :is-loading="store.isLoading"
              @select="handleAchievementSelect"
            />

            <!-- Leaderboard Tab -->
            <LeaderboardSection
              v-else-if="activeTab === 'leaderboard'"
              :entries="store.leaderboard"
              :current-user-id="userId"
              :is-loading="store.isLoading"
              :metric="leaderboardMetric"
              @update:metric="handleMetricChange"
              @view-profile="(id) => console.log('View profile:', id)"
            />

          </div>
        </template>
      </div>

      <!-- Achievement Detail Modal -->
      <Transition name="modal">
        <div 
          v-if="selectedAchievement" 
          class="fixed inset-0 z-[60] flex items-center justify-center p-4"
        >
          <div class="absolute inset-0 bg-ink-900/60 backdrop-blur-sm" @click="selectedAchievement = null"></div>
          <div class="relative w-full max-w-md bg-surface-0 rounded-xl shadow-card p-6 z-10">
            <button 
              @click="selectedAchievement = null"
              class="absolute top-4 right-4 text-ink-300 hover:text-ink-500 p-1"
            >
              <X :size="18" />
            </button>
            
            <div class="flex items-start gap-4">
              <div 
                class="w-16 h-16 rounded-2xl flex items-center justify-center shrink-0"
                :class="selectedAchievement.isUnlocked 
                  ? 'bg-brand-100 text-brand-600' 
                  : 'bg-surface-100 text-ink-300'"
              >
                <component :is="selectedAchievement.icon || 'Star'" :size="28" />
              </div>
              <div class="flex-1">
                <h3 class="text-lg font-semibold text-ink-900">{{ selectedAchievement.name }}</h3>
                <p class="text-sm text-ink-500 mt-1">{{ selectedAchievement.description }}</p>
                
                <div class="flex items-center gap-3 mt-4">
                  <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-sm font-medium bg-amber-50 text-amber-700">
                    <Trophy :size="12" />
                    +{{ selectedAchievement.points }} XP
                  </span>
                  <span 
                    v-if="selectedAchievement.isUnlocked && selectedAchievement.achievedAt"
                    class="text- text-ink-400"
                  >
                    Unlocked {{ new Date(selectedAchievement.achievedAt).toLocaleDateString() }}
                  </span>
                </div>

                <!-- Progress for locked -->
                <div v-if="!selectedAchievement.isUnlocked" class="mt-5">
                  <div class="flex justify-between text-sm text-ink-500 mb-2">
                    <span>Progress</span>
                    <span>{{ selectedAchievement.percentage }}%</span>
                  </div>
                  <div class="h-2 bg-surface-100 rounded-full overflow-hidden">
                    <div 
                      class="h-full bg-brand-400 rounded-full transition-all"
                      :style="{ width: `${selectedAchievement.percentage}%` }"
                    ></div>
                  </div>
                  <p class="text-[15px] text-ink-400 mt-2">
                    {{ selectedAchievement.progress }} / {{ selectedAchievement.targetValue }} completed
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Transition>

    </div>
  </Transition>
</template>

<style scoped>
/* Overlay Transitions */
.overlay-enter-active,
.overlay-leave-active {
  transition: opacity 0.25s ease;
}
.overlay-enter-from,
.overlay-leave-to {
  opacity: 0;
}
.overlay-enter-active .relative {
  transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.3s ease;
}
.overlay-enter-from .relative,
.overlay-leave-to .relative {
  opacity: 0;
  transform: translateY(16px) scale(0.98);
}

/* Achievement Detail Modal */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
.modal-enter-active > div:last-child {
  transition: transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}
.modal-enter-from > div:last-child,
.modal-leave-to > div:last-child {
  transform: scale(0.95) translateY(8px);
}
</style>