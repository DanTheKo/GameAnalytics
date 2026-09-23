import { setActivePinia, createPinia } from 'pinia'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useGamificationStore } from '../gamification'
import * as api from '../../api/gamification'

// Мокаем API функции
vi.mock('../../api/gamification', () => ({
  getGamificationProfile: vi.fn(),
  trackEvent: vi.fn(),
  checkNewAchievements: vi.fn(),
  getLeaderboard: vi.fn(),
}))

describe('Gamification Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  describe('Initial State', () => {
    it('should have default state values', () => {
      const store = useGamificationStore()
      
      expect(store.profile).toBeNull()
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
      expect(store.leaderboard).toEqual([])
      expect(store.newAchievements).toEqual([])
    })
  })

  describe('Computed Properties', () => {
    it('should compute xpPercentage correctly', async () => {
      const store = useGamificationStore()
      
      // Mock profile with progress
      store.profile = {
        user: { id: '1', name: 'Test', createdAt: '2024-01-01' },
        progress: {
          userId: '1',
          level: 1,
          currentXp: 250,
          nextLevelXp: 500,
          totalEvents: 10,
          eventStats: { login: 10 },
        },
        achievements: [],
        recentEvents: [],
      }

      expect(store.xpPercentage).toBe(50)
    })

    it('should return 0 when no progress', () => {
      const store = useGamificationStore()
      expect(store.xpPercentage).toBe(0)
    })

    it('should compute unlockedAchievementsCount', () => {
      const store = useGamificationStore()
      store.profile = {
        user: { id: '1', name: 'Test', createdAt: '2024-01-01' },
        progress: null as any,
        achievements: [
          { id: '1', name: 'Ach1', description: '', isUnlocked: true, progress: 100, targetValue: 100, percentage: 100, createdAt: '2024-01-01' },
          { id: '2', name: 'Ach2', description: '', isUnlocked: false, progress: 50, targetValue: 100, percentage: 50, createdAt: '2024-01-01' },
          { id: '3', name: 'Ach3', description: '', isUnlocked: true, progress: 100, targetValue: 100, percentage: 100, createdAt: '2024-01-01' },
        ],
        recentEvents: [],
      }

      expect(store.unlockedAchievementsCount).toBe(2)
    })

    it('should compute user from profile', () => {
      const store = useGamificationStore()
      const mockUser = { id: '1', name: 'Test User', createdAt: '2024-01-01' }
      store.profile = {
        user: mockUser,
        progress: null as any,
        achievements: [],
        recentEvents: [],
      }

      expect(store.user).toEqual(mockUser)
    })
  })

  describe('Actions', () => {
    const mockProfile = {
      user: { id: 'user-123', name: 'Test User', createdAt: '2024-01-01' },
      progress: {
        userId: 'user-123',
        level: 1,
        currentXp: 100,
        nextLevelXp: 500,
        totalEvents: 5,
        eventStats: { login: 5 },
      },
      achievements: [],
      recentEvents: [],
    }

    describe('loadProfile', () => {
      it('should load profile successfully', async () => {
        vi.mocked(api.getGamificationProfile).mockResolvedValue(mockProfile)
        
        const store = useGamificationStore()
        await store.loadProfile('user-123', 'testuser')

        expect(api.getGamificationProfile).toHaveBeenCalledWith('user-123', 'testuser')
        expect(store.profile).toEqual(mockProfile)
        expect(store.isLoading).toBe(false)
        expect(store.error).toBeNull()
      })


      it('should set loading state during fetch', async () => {
        vi.mocked(api.getGamificationProfile).mockImplementation(
          () => new Promise(resolve => setTimeout(() => resolve(mockProfile), 100))
        )
        
        const store = useGamificationStore()
        const loadPromise = store.loadProfile('user-123', 'testuser')
        
        expect(store.isLoading).toBe(true)
        
        await loadPromise
        expect(store.isLoading).toBe(false)
      })
    })

    describe('emitEvent', () => {
      const mockEvent = {
        id: 'event-1',
        userId: 'user-123',
        eventType: 'login',
        value: 1,
        createdAt: new Date().toISOString(),
      }

      it('should track event and update local state optimistically', async () => {
        vi.mocked(api.trackEvent).mockResolvedValue(mockEvent)
        vi.mocked(api.checkNewAchievements).mockResolvedValue([])
        
        const store = useGamificationStore()
        store.profile = { ...mockProfile, recentEvents: [] }
        
        await store.emitEvent('user-123', 'login', 1)

        expect(api.trackEvent).toHaveBeenCalledWith('user-123', 'login', 1)
        expect(store.profile?.recentEvents[0]).toEqual(mockEvent)
        expect(store.profile?.progress?.totalEvents).toBe(6)
        expect(store.profile?.progress?.currentXp).toBe(110) // 100 + 1*10
      })

      it('should check achievements after event', async () => {
        vi.mocked(api.trackEvent).mockResolvedValue(mockEvent)
        vi.mocked(api.checkNewAchievements).mockResolvedValue([])
        
        const store = useGamificationStore()
        store.profile = mockProfile
        
        await store.emitEvent('user-123', 'login')

        expect(api.checkNewAchievements).toHaveBeenCalledWith('user-123')
      })

      it('should throw error when tracking fails', async () => {
        vi.mocked(api.trackEvent).mockRejectedValue(new Error('Track failed'))
        
        const store = useGamificationStore()
        
        await expect(store.emitEvent('user-123', 'login')).rejects.toThrow('Track failed')
      })
    })

    describe('checkNewAchievementsAfterEvent', () => {
      const mockNewAchievements = [
        {
          id: 'ach-1',
          name: 'New Achievement',
          description: 'Test',
          isUnlocked: true,
          achievedAt: new Date().toISOString(),
          progress: 100,
          targetValue: 100,
          percentage: 100,
          createdAt: '2024-01-01',
        },
      ]

      it('should add new achievements to profile', async () => {
        vi.mocked(api.checkNewAchievements).mockResolvedValue(mockNewAchievements)
        
        const store = useGamificationStore()
        store.profile = { ...mockProfile, achievements: [] }
        
        await store.checkNewAchievementsAfterEvent('user-123')

        expect(store.newAchievements).toEqual(mockNewAchievements)
        expect(store.profile?.achievements.length).toBe(1)
      })

      it('should update existing achievement if already in profile', async () => {
        const existingAch = {
          id: 'ach-1',
          name: 'Old Name',
          description: 'Old',
          isUnlocked: false,
          progress: 50,
          targetValue: 100,
          percentage: 50,
          createdAt: '2024-01-01',
        }
        
        vi.mocked(api.checkNewAchievements).mockResolvedValue([
          { ...mockNewAchievements[0], isUnlocked: true, progress: 100 }
        ])
        
        const store = useGamificationStore()
        store.profile = { ...mockProfile, achievements: [existingAch] }
        
        await store.checkNewAchievementsAfterEvent('user-123')

        expect(store.profile?.achievements[0].isUnlocked).toBe(true)
      })

      it('should handle error gracefully', async () => {
        vi.mocked(api.checkNewAchievements).mockRejectedValue(new Error('Check failed'))
        
        const store = useGamificationStore()
        
        await expect(store.checkNewAchievementsAfterEvent('user-123')).resolves.not.toThrow()
      })
    })

    describe('loadLeaderboard', () => {
      const mockLeaderboard = [
        { userId: '1', name: 'Player 1', value: 1000, rank: 1 },
        { userId: '2', name: 'Player 2', value: 900, rank: 2 },
      ]

      it('should load leaderboard with default metric', async () => {
        vi.mocked(api.getLeaderboard).mockResolvedValue(mockLeaderboard)
        
        const store = useGamificationStore()
        await store.loadLeaderboard()

        expect(api.getLeaderboard).toHaveBeenCalledWith('xp', 10)
        expect(store.leaderboard).toEqual(mockLeaderboard)
      })

      it('should load leaderboard with custom metric and limit', async () => {
        vi.mocked(api.getLeaderboard).mockResolvedValue(mockLeaderboard)
        
        const store = useGamificationStore()
        await store.loadLeaderboard('level', 20)

        expect(api.getLeaderboard).toHaveBeenCalledWith('level', 20)
      })

      it('should handle error when loading leaderboard', async () => {
        vi.mocked(api.getLeaderboard).mockRejectedValue(new Error('Failed'))
        
        const store = useGamificationStore()
        await store.loadLeaderboard()

        expect(store.leaderboard).toEqual([])
      })
    })

    describe('reset', () => {
      it('should reset all state to default values', () => {
        const store = useGamificationStore()
        store.profile = mockProfile
        store.leaderboard = [{ userId: '1', name: 'Test', value: 100, rank: 1 }]
        store.newAchievements = [{ id: '1', name: 'Test', description: '', isUnlocked: true, progress: 100, targetValue: 100, percentage: 100, createdAt: '2024-01-01' }]
        store.error = 'Some error'

        store.reset()

        expect(store.profile).toBeNull()
        expect(store.leaderboard).toEqual([])
        expect(store.newAchievements).toEqual([])
        expect(store.error).toBeNull()
      })
    })
  })
})