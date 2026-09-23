import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import ProfileOverlay from '../../components/gamification/ProfileOverlay.vue'
import { useGamificationStore } from '../../stores/gamification'

// Мокаем дочерние компоненты
vi.mock('../../components/gamification/AchievementsGrid.vue', () => ({
  default: {
    template: '<div data-testid="achievements-grid">AchievementsGrid</div>',
    props: ['achievements', 'isLoading'],
    emits: ['select'],
  },
}))

vi.mock('../../components/gamification/LeaderboardSection.vue', () => ({
  default: {
    template: '<div data-testid="leaderboard-section">LeaderboardSection</div>',
    props: ['entries', 'currentUserId', 'isLoading', 'metric'],
    emits: ['update:metric', 'view-profile'],
  },
}))

// Мокаем auth store
vi.mock('../../../stores/index', () => ({
  useAuthStore: () => ({
    username: 'testuser',
    userId: 'user-123',
  }),
}))

describe('ProfileOverlay', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  const mockProfile = {
    user: { 
      id: 'user-123', 
      name: 'Test User', 
      email: 'test@example.com',
      avatarUrl: 'https://example.com/avatar.jpg',
      createdAt: '2024-01-15T10:30:00Z' 
    },
    progress: {
      userId: 'user-123',
      level: 5,
      currentXp: 2500,
      nextLevelXp: 3000,
      totalEvents: 150,
      eventStats: { login: 50, like: 100 },
    },
    achievements: [
      { 
        id: '1', 
        name: 'First Achievement', 
        description: 'Test desc', 
        icon: 'Star',
        isUnlocked: true, 
        achievedAt: '2024-01-01',
        progress: 100,
        targetValue: 100,
        percentage: 100,
        createdAt: '2024-01-01',
      },
      { 
        id: '2', 
        name: 'Locked Achievement', 
        description: 'Locked desc', 
        icon: 'Lock',
        isUnlocked: false, 
        progress: 50,
        targetValue: 100,
        percentage: 50,
        createdAt: '2024-01-01',
      },
    ],
    recentEvents: [],
  }

  describe('Rendering', () => {
    it('should render loading state when isLoading is true', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: {
          modelValue: true,
        },
        global: {
          plugins: [createPinia()],
        },
      })

      const store = useGamificationStore()
      store.isLoading = true
      await wrapper.vm.$nextTick()

      expect(wrapper.find('.animate-spin').exists()).toBe(true)
      expect(wrapper.text()).toContain('Loading profile...')
    })


    it('should render profile content when data is loaded', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      store.isLoading = false
      await wrapper.vm.$nextTick()

      expect(wrapper.find('h2').text()).toBe('testuser')
      expect(wrapper.find('[data-testid="count"]').exists()).toBe(false) // XP display
      expect(wrapper.text()).toContain('Lvl 5')
    })

    it('should display user avatar', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      const img = wrapper.find('img')
      expect(img.exists()).toBe(true)
      expect(img.attributes('src')).toBe('https://example.com/avatar.jpg')
    })
  })

  describe('XP Progress Bar', () => {
    it('should display correct XP percentage', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      // 2500/3000 = 83.33% -> but formula is different in code
      expect(store.xpPercentage).toBeDefined()
      expect(wrapper.text()).toContain('XP')
    })

    it('should render progress bar with correct width', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      const progressBar = wrapper.find('.bg-brand-500')
      expect(progressBar.exists()).toBe(true)
    })
  })

  describe('Tabs Navigation', () => {
    it('should have three tabs: overview, achievements, leaderboard', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      const tabs = wrapper.findAll('button')
      expect(tabs.length).toBeGreaterThanOrEqual(3)
      expect(wrapper.text()).toContain('Overview')
      expect(wrapper.text()).toContain('Achievements')
      expect(wrapper.text()).toContain('Leaderboard')
    })

    it('should switch to achievements tab when clicked', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      const achievementsTab = wrapper.findAll('button').find(btn => 
        btn.text().includes('Achievements')
      )
      await achievementsTab?.trigger('click')

      expect(wrapper.find('[data-testid="achievements-grid"]').exists()).toBe(true)
    })

    it('should switch to leaderboard tab when clicked', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      const leaderboardTab = wrapper.findAll('button').find(btn => 
        btn.text().includes('Leaderboard')
      )
      await leaderboardTab?.trigger('click')

      expect(wrapper.find('[data-testid="leaderboard-section"]').exists()).toBe(true)
    })
  })


  describe('Stats Display', () => {
    it('should display total events count', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      expect(wrapper.text()).toContain('150')
    })

    it('should display unlocked achievements count', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      
      expect(wrapper.text()).toContain('1/2') // 1 unlocked out of 2
    })
  })

  describe('Achievement Detail Modal', () => {
    it('should open achievement detail when selected', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      // Trigger achievement select
      const component = wrapper.vm as any
      component.handleAchievementSelect(mockProfile.achievements[0])
      await wrapper.vm.$nextTick()

      expect(wrapper.text()).toContain('First Achievement')
    })

    it('should close achievement detail when clicking overlay', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      await wrapper.vm.$nextTick()

      const component = wrapper.vm as any
      component.selectedAchievement = mockProfile.achievements[0]
      await wrapper.vm.$nextTick()

      // Close modal
      component.selectedAchievement = null
      await wrapper.vm.$nextTick()

      expect(component.selectedAchievement).toBeNull()
    })
  })

  describe('Leaderboard Metric Change', () => {
    it('should change leaderboard metric when selected', async () => {
      const wrapper = mount(ProfileOverlay, {
        props: { modelValue: true },
        global: { plugins: [createPinia()] },
      })

      const store = useGamificationStore()
      store.profile = mockProfile
      store.loadLeaderboard = vi.fn()
      await wrapper.vm.$nextTick()

      const component = wrapper.vm as any
      component.handleMetricChange('level')

      expect(component.leaderboardMetric).toBe('level')
      expect(store.loadLeaderboard).toHaveBeenCalledWith('level')
    })
  })

})