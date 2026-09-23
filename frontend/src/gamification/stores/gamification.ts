import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Guid, GamificationProfile, AchievementWithStatus, GamificationEvent } from '../api/types'
import {
  getGamificationProfile,
  trackEvent,
  checkNewAchievements,
  getLeaderboard,
} from '../api/gamification'

export interface LeaderboardEntry {
  userId: Guid
  name: string
  avatarUrl?: string
  value: number
  rank: number
}

export const useGamificationStore = defineStore('gamification', () => {
  // State
  const profile = ref<GamificationProfile | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const leaderboard = ref<LeaderboardEntry[]>([])
  const newAchievements = ref<AchievementWithStatus[]>([])

  // Computed
  const user = computed(() => profile.value?.user)
  const progress = computed(() => profile.value?.progress)
  const achievements = computed(() => profile.value?.achievements || [])
  const recentEvents = computed(() => profile.value?.recentEvents || [])
  
  const xpPercentage = computed(() => {
    if (!progress.value) return 0
    return Math.min(100, Math.round((500 - (progress.value.nextLevelXp - progress.value.currentXp))/500 * 100))
  })

  const unlockedAchievementsCount = computed(() => 
    achievements.value.filter(a => a.isUnlocked).length
  )

  // Actions
  async function loadProfile(userId: Guid, username: string) {
    isLoading.value = true
    error.value = null
    
    try {
      const data = await getGamificationProfile(userId, username)
      profile.value = data
    } catch (e) {
      error.value = 'Failed to load profile'
      console.error(error.value)
    } finally {
      isLoading.value = false
    }
  }

  async function emitEvent(userId: Guid, eventType: string, value: number = 1) {
    try {
      const newEvent = await trackEvent(userId, eventType, value)
      
      // Optimistically update local state
      if (profile.value) {
        profile.value.recentEvents.unshift(newEvent)
        
        // Update progress stats
        if (progress.value) {
          progress.value.eventStats[eventType] = 
            (progress.value.eventStats[eventType] || 0) + value
          progress.value.totalEvents += value
          progress.value.currentXp += value * 10
        }
      }

      // Check for new achievements
      await checkNewAchievementsAfterEvent(userId)
      
      return newEvent
    } catch (e) {
      console.error('Failed to track event:', e)
      throw e
    }
  }

  async function checkNewAchievementsAfterEvent(userId: Guid) {
    try {
      const newAchs = await checkNewAchievements(userId)
      if (newAchs.length > 0) {
        newAchievements.value = newAchs
        
        // Add to profile achievements
        if (profile.value) {
          for (const ach of newAchs) {
            const existing = profile.value.achievements.find(a => a.id === ach.id)
            if (existing) {
              Object.assign(existing, ach)
            } else {
              profile.value.achievements.push(ach)
            }
          }
        }
      }
    } catch (e) {
      console.error('Failed to check achievements:', e)
    }
  }
  

  async function loadLeaderboard(metric: 'xp' | 'level' | 'achievements' = 'xp', limit: number = 10) {
    try {
      const response = await getLeaderboard(metric, limit)
      leaderboard.value = response
    } catch (e) {
      console.error('Failed to load leaderboard:', e)
    }
  }

  function reset() {
    profile.value = null
    leaderboard.value = []
    newAchievements.value = []
    error.value = null
  }

  return {
    // State
    profile,
    isLoading,
    error,
    leaderboard,
    newAchievements,
    // Computed
    user,
    progress,
    achievements,
    recentEvents,
    xpPercentage,
    unlockedAchievementsCount,
    // Actions
    loadProfile,
    emitEvent,
    checkNewAchievementsAfterEvent,
    loadLeaderboard,
    reset,
  }
})


export { getLeaderboard, checkNewAchievements, trackEvent, getGamificationProfile }
