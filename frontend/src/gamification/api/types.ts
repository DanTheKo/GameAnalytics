// Guid type for consistency
export type Guid = string

// ==================== User ====================
export interface User {
  id: Guid
  name: string
  email?: string
  avatarUrl? : string
  createdAt: string // datetime
}

// ==================== Events ====================
export type EventType = 
  | 'login' 
  | 'purchase' 
  | 'share' 
  | 'comment' 
  | 'like' 
  | 'achievement_unlocked'
  | 'level_up'
  | string // Allow custom events

export interface GamificationEvent {
  id: Guid
  userId: Guid
  eventType: EventType
  value: number
  createdAt: string // datetime
}

export interface UserEventProgress {
  id: Guid
  userId: Guid
  eventType: EventType
  value: number // Aggregated value for this event type
  updatedAt: string
}

// ==================== Achievements ====================
export interface Achievement {
  id: Guid
  name: string
  description: string
  icon?: string // Lucide icon name
  points?: number // XP reward
  createdAt: string
}

export interface AchievementCondition {
  id: Guid
  achievementId: Guid
  eventType: EventType
  targetValue: number
}

export interface UserAchievement {
  id: Guid
  userId: Guid
  achievementId: Guid
  achievedAt: string // datetime
  achievement: Achievement // Included for convenience
}

// ==================== Aggregated Responses ====================
export interface UserProgressSummary {
  userId: Guid
  level: number
  currentXp: number
  nextLevelXp: number
  totalEvents: number
  eventStats: Record<EventType, number>
  rank?: number
}

export interface AchievementWithStatus extends Achievement {
  isUnlocked: boolean
  achievedAt?: string
  progress: number // Current progress towards condition
  targetValue: number // Required value for condition
  percentage: number // 0-100
}

export interface GamificationProfile {
  user: User
  progress: UserProgressSummary
  achievements: AchievementWithStatus[]
  recentEvents: GamificationEvent[]
}

// ==================== API Response Wrappers ====================
export interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
}

export interface PaginatedResponse<T> {
  data: T[]
  total: number
  page: number
  pageSize: number
  hasMore: boolean
}
export interface LeaderboardResponse {
  data: LeaderboardEntry[]
  total: number
  page: number
  pageSize: number
  hasMore: boolean
}

export interface LeaderboardEntry {
  userId: Guid
  name: string
  avatarUrl?: string
  value: number
  rank: number
}

// ==================== Request Params ====================
export interface EventFilterParams {
  userId: Guid
  startDate?: string
  endDate?: string
  eventType?: EventType
  page?: number
  pageSize?: number
  [key: string]: string | number | boolean | undefined
}

export interface AchievementFilterParams {
  userId: Guid
  includeLocked?: boolean
  limit?: number
  [key: string]: string | number | boolean | undefined
}