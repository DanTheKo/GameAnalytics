import type {
  Guid,
  GamificationProfile,
  GamificationEvent,
  AchievementWithStatus,
  UserProgressSummary,
  EventFilterParams,
  AchievementFilterParams,
  PaginatedResponse,
  LeaderboardResponse,
  LeaderboardEntry
} from './types'
import { apiGet, apiPost, API_CONFIG } from './client'

// ==================== Mock Data ====================
const MOCK_USER_ID: Guid = '550e8400-e29b-41d4-a716-446655440000'

const mockProfile: GamificationProfile = {
  user: {
    id: MOCK_USER_ID,
    name: 'Alex Designer',
    email: 'alex@example.com',
    avatarUrl: '',
    createdAt: '2023-01-15T10:30:00Z',
  },
  progress: {
    userId: MOCK_USER_ID,
    level: 5,
    currentXp: 2450,
    nextLevelXp: 3000,
    totalEvents: 156,
    eventStats: {
      login: 45,
      purchase: 12,
      share: 8,
      comment: 34,
      like: 57,
    },
    rank: 23,
  },
  achievements: [
    {
      id: '1',
      name: 'Early Bird',
      description: 'Login 7 days in a row',
      icon: 'Sun',
      points: 100,
      createdAt: '2023-01-01T00:00:00Z',
      isUnlocked: true,
      achievedAt: '2023-10-01T08:15:00Z',
      progress: 7,
      targetValue: 7,
      percentage: 100,
    },
    {
      id: '2',
      name: 'Big Spender',
      description: 'Make 10 purchases',
      icon: 'ShoppingCart',
      points: 250,
      createdAt: '2023-01-01T00:00:00Z',
      isUnlocked: true,
      achievedAt: '2023-10-05T14:30:00Z',
      progress: 12,
      targetValue: 10,
      percentage: 100,
    },
    {
      id: '3',
      name: 'Social Star',
      description: 'Share 50 times',
      icon: 'Share2',
      points: 500,
      createdAt: '2023-01-01T00:00:00Z',
      isUnlocked: false,
      progress: 8,
      targetValue: 50,
      percentage: 16,
    },
    {
      id: '4',
      name: 'Master',
      description: 'Reach Level 10',
      icon: 'Trophy',
      points: 1000,
      createdAt: '2023-01-01T00:00:00Z',
      isUnlocked: false,
      progress: 5,
      targetValue: 10,
      percentage: 50,
    },
    {
      id: '5',
      name: 'Commentator',
      description: 'Write 100 comments',
      icon: 'MessageSquare',
      points: 300,
      createdAt: '2023-01-01T00:00:00Z',
      isUnlocked: false,
      progress: 34,
      targetValue: 100,
      percentage: 34,
    },
  ],
  recentEvents: [
    { id: 'e1', userId: MOCK_USER_ID, eventType: 'login', value: 1, createdAt: new Date().toISOString() },
    { id: 'e2', userId: MOCK_USER_ID, eventType: 'like', value: 5, createdAt: new Date(Date.now() - 3600000).toISOString() },
    { id: 'e3', userId: MOCK_USER_ID, eventType: 'comment', value: 2, createdAt: new Date(Date.now() - 7200000).toISOString() },
    { id: 'e4', userId: MOCK_USER_ID, eventType: 'purchase', value: 1, createdAt: new Date(Date.now() - 86400000).toISOString() },
    { id: 'e5', userId: MOCK_USER_ID, eventType: 'share', value: 1, createdAt: new Date(Date.now() - 172800000).toISOString() },
  ],
}

// Simulate network delay
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

// ==================== API Functions ====================

/**
 * Get complete gamification profile for a user
 */
export async function getGamificationProfile(userId: Guid, username: string): Promise<GamificationProfile> {
  if (API_CONFIG.useMock) {
    await delay(600) // Simulate network latency
    // In real app, you might filter by userId
    return { ...mockProfile, user: { ...mockProfile.user, id: userId } }
  }

  const response = await apiGet<GamificationProfile>(`/gamification/profile/${userId}?username=${username}`)
  if (!response.success) throw new Error(response.message)
  return response.data
}

/**
 * Get user's achievements with progress status
 */
export async function getUserAchievements(
  userId: Guid,
  params?: AchievementFilterParams
): Promise<AchievementWithStatus[]> {
  if (API_CONFIG.useMock) {
    await delay(400)
    return mockProfile.achievements
  }

  const response = await apiGet<AchievementWithStatus[]>(
    `/gamification/achievements`,
    { userId, ...params }
  )
  if (!response.success) throw new Error(response.message)
  return response.data
}

/**
 * Get user's progress summary
 */
export async function getUserProgress(userId: Guid): Promise<UserProgressSummary> {
  if (API_CONFIG.useMock) {
    await delay(300)
    return mockProfile.progress
  }

  const response = await apiGet<UserProgressSummary>(`/gamification/progress/${userId}`)
  if (!response.success) throw new Error(response.message)
  return response.data
}

/**
 * Get paginated events for a user
 */
export async function getUserEvents(params: EventFilterParams): Promise<PaginatedResponse<GamificationEvent>> {
  if (API_CONFIG.useMock) {
    await delay(500)
    const page = params.page || 1
    const pageSize = params.pageSize || 10
    
    // Filter mock events
    let filtered = [...mockProfile.recentEvents]
    
    if (params.eventType) {
      filtered = filtered.filter(e => e.eventType === params.eventType)
    }
    
    if (params.startDate) {
      filtered = filtered.filter(e => new Date(e.createdAt) >= new Date(params.startDate!))
    }
    
    if (params.endDate) {
      filtered = filtered.filter(e => new Date(e.createdAt) <= new Date(params.endDate!))
    }

    const total = filtered.length
    const start = (page - 1) * pageSize
    const end = start + pageSize
    const paginated = filtered.slice(start, end)

    return {
      data: paginated,
      total,
      page,
      pageSize,
      hasMore: end < total,
    }
  }

  const response = await apiGet<PaginatedResponse<GamificationEvent>>(
    '/gamification/events', params
  )
  if (!response.success) throw new Error(response.message)
  return response.data
}

/**
 * Track a new gamification event
 */
export async function trackEvent(
  userId: Guid,
  eventType: string,
  value: number = 1
): Promise<GamificationEvent> {
  if (API_CONFIG.useMock) {
    await delay(200)
    
    const newEvent: GamificationEvent = {
      id: crypto.randomUUID(),
      userId,
      eventType,
      value,
      createdAt: new Date().toISOString(),
    }
    
    // Add to mock recent events
    mockProfile.recentEvents.unshift(newEvent)
    
    // Update progress (simplified logic)
    if (mockProfile.progress.eventStats[eventType]) {
      mockProfile.progress.eventStats[eventType] += value
    } else {
      mockProfile.progress.eventStats[eventType] = value
    }
    mockProfile.progress.totalEvents += value
    mockProfile.progress.currentXp += value * 10
    
    return newEvent
  }

  const response = await apiPost<GamificationEvent>('/gamification/events', {
    userId,
    eventType,
    value,
  })
  if (!response.success) throw new Error(response.message)
  return response.data
}

/**
 * Check if user unlocked any new achievements
 */
export async function checkNewAchievements(userId: Guid): Promise<AchievementWithStatus[]> {
  if (API_CONFIG.useMock) {
    await delay(300)
    // Return empty array (no new achievements in mock)
    return []
  }

  const response = await apiPost<AchievementWithStatus[]>(
    '/gamification/achievements/check',
    { userId }
  )
  if (!response.success) throw new Error(response.message)
  return response.data
}


export async function getLeaderboard(
  metric: 'xp' | 'level' | 'achievements',
  limit: number = 10
): Promise<LeaderboardEntry[]> { 

  const response = await apiGet<LeaderboardEntry[]>(
    '/gamification/leaderboard',
    { metric, limit }
  )

  if (!response.success) throw new Error(response.message)
  
  return response.data
}