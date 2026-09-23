import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import AppHeader from '../AppHeader.vue'
import { useGamificationStore } from '../../gamification/stores/gamification'
import * as gamificationApi from '../../gamification/api/gamification'
import { useProjectStore, useAuthStore } from '../../stores/index'




// Мокаем API
vi.mock('../../gamification/api/gamification', () => ({
  getGamificationProfile: vi.fn(),
  trackEvent: vi.fn(),
  checkNewAchievements: vi.fn(),
  getLeaderboard: vi.fn(),
}))


vi.mock('../../stores/index', () => ({
  useProjectStore: () => ({
    projects: [],
    fetchProjects: vi.fn(),
  }),
    useAuthStore: () => ({
    username: 'testuser',
    userId: 'user-123',
  }),
}))



describe('Gamification Integration', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  const mockProfile = {
    user: { id: 'user-123', name: 'Test User', createdAt: '2026-01-01' },
    progress: {
      userId: 'user-123',
      level: 5,
      currentXp: 2500,
      nextLevelXp: 3000,
      totalEvents: 150,
      eventStats: { login: 50 },
    },
    achievements: [],
    recentEvents: [],
  }

  it('should load profile when opening overlay from header', async () => {
    vi.mocked(gamificationApi.getGamificationProfile).mockResolvedValue(mockProfile)
    const wrapper = mount(AppHeader, {
      global: {
        plugins: [createPinia()],
      },
    })

    const badge = wrapper.find('[data-testid="gamification-badge"]')
    await badge.trigger('click')

    expect(gamificationApi.getGamificationProfile).toHaveBeenCalledWith('user-123', 'testuser')
  })

  it('should update XP after event tracking', async () => {
    vi.mocked(gamificationApi.trackEvent).mockResolvedValue({
      id: 'event-1',
      userId: 'user-123',
      eventType: 'login',
      value: 1,
      createdAt: new Date().toISOString(),
    })
    
    vi.mocked(gamificationApi.checkNewAchievements).mockResolvedValue([])

    const store = useGamificationStore()
    store.profile = mockProfile

    await store.emitEvent('user-123', 'login', 1)

    expect(store.profile?.progress?.currentXp).toBe(2510) // 2500 + 1*10
    expect(store.profile?.progress?.totalEvents).toBe(151)
  })

  it('should show new achievements indicator after unlock', async () => {
    vi.mocked(gamificationApi.checkNewAchievements).mockResolvedValue([
      {
        id: 'ach-1',
        name: 'New Achievement',
        description: 'Test',
        icon: 'Star',
        points: 100,
        isUnlocked: true,
        progress: 100,
        targetValue: 100,
        percentage: 100,
        createdAt: '2026-01-01',
      },
    ])

    const store = useGamificationStore()
    store.profile = mockProfile
    store.newAchievements = []

    await store.checkNewAchievementsAfterEvent('user-123')

    expect(store.newAchievements.length).toBe(1)
  })

  it('should persist achievements in profile after unlock', async () => {
    const newAchievement = {
      id: 'ach-1',
      name: 'New Achievement',
      description: 'Test',
      icon: 'Star',
      points: 100,
      isUnlocked: true,
      progress: 100,
      targetValue: 100,
      percentage: 100,
      createdAt: '2026-01-01',
    }

    vi.mocked(gamificationApi.checkNewAchievements).mockResolvedValue([newAchievement])

    const store = useGamificationStore()
    store.profile = mockProfile

    await store.checkNewAchievementsAfterEvent('user-123')

    expect(store.profile?.achievements).toContainEqual(
      expect.objectContaining({ id: 'ach-1', isUnlocked: true })
    )
  })

  it('should handle API errors gracefully', async () => {
    vi.mocked(gamificationApi.getGamificationProfile).mockRejectedValue(new Error('Network error'))

    const store = useGamificationStore()
    await store.loadProfile('user-123', 'testuser')

    expect(store.error).toBe('Failed to load profile')
    expect(store.isLoading).toBe(false)
  })

  it('should reset state on logout', async () => {
    const store = useGamificationStore()
    store.profile = mockProfile
    store.leaderboard = [{ userId: '1', name: 'Test', value: 100, rank: 1 }]
    store.newAchievements = [{ id: '1', name: 'Test', description: '', isUnlocked: true, progress: 100, targetValue: 100, percentage: 100, createdAt: '2026-01-01' }]

    store.reset()

    expect(store.profile).toBeNull()
    expect(store.leaderboard).toEqual([])
    expect(store.newAchievements).toEqual([])
    expect(store.error).toBeNull()
  })
})