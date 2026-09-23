/**
 * Metrics API tests
 *
 * Stack: Vitest + MSW (Mock Service Worker) for request interception.
 *
 * Install (if not already present):
 *   npm i -D vitest msw @vitest/coverage-v8
 *
 * Run:
 *   npx vitest run src/api/__tests__/metrics.test.ts
 */

import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest'
import { http, HttpResponse }                                     from 'msw'
import { setupServer }                                            from 'msw/node'

// Adjust this import path to wherever your api module lives
import { metricsApi, last30Days, toIso } from './index'

// ─────────────────────────────────────────────────────────────
//  Fixture data
// ─────────────────────────────────────────────────────────────

const PROJECT_ID = 'proj-123'
const BASE_URL   = 'http://localhost:5878'   // match your test env / vite config

const mockDauWauMau = {
  dau: [{ date: '2024-01-01T00:00:00Z', value: 5 }],
  wau: [{ date: '2024-01-01T00:00:00Z', value: 12 }],
  mau: [{ date: '2024-01-01T00:00:00Z', value: 40 }],
}

const mockRevenue = {
  dailyRevenue:        [{ date: '2024-01-01T00:00:00Z', value: 99.99 }],
  totalRevenue:        99.99,
  arpu:                4.99,
  arppu:               19.99,
  payingUsers:         5,
  payerConversionRate: 25.0,
}

const mockRetention = {
  classic:     [{ day: 1, rate: 45.5 }, { day: 7, rate: 22.1 }],
  rolling:     [{ day: 1, rate: 60.0 }, { day: 7, rate: 35.0 }],
  cohortTable: [
    {
      cohortLabel:    'Jan 01',
      cohortSize:     100,
      retentionRates: [45.5, null, 22.1],
    },
  ],
}

const mockSessions = {
  avgSessionDuration:        [{ date: '2024-01-01T00:00:00Z', value: 123.4 }],
  sessionsPerUser:           [{ date: '2024-01-01T00:00:00Z', value: 2.3  }],
  overallAvgDurationSeconds: 123.4,
}

// ─────────────────────────────────────────────────────────────
//  MSW server
// ─────────────────────────────────────────────────────────────

/**
 * Builds the handler URL pattern so we don't repeat the path logic.
 * Adjust the path prefix to match your actual backend route structure.
 */
const metricsPath = (segment: string) =>
  `${BASE_URL}/api/projects/${PROJECT_ID}/metrics/${segment}`

const server = setupServer(
  http.get(metricsPath('dau-wau-mau'), () => HttpResponse.json(mockDauWauMau)),
  http.get(metricsPath('revenue'),     () => HttpResponse.json(mockRevenue)),
  http.get(metricsPath('retention'),   () => HttpResponse.json(mockRetention)),
  http.get(metricsPath('sessions'),    () => HttpResponse.json(mockSessions)),
)

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => server.resetHandlers())
afterAll(()  => server.close())

// ─────────────────────────────────────────────────────────────
//  Helper
// ─────────────────────────────────────────────────────────────

const { from, to } = last30Days()

// ─────────────────────────────────────────────────────────────
//  getDauWauMau
// ─────────────────────────────────────────────────────────────

describe('metricsApi.getDauWauMau', () => {
  it('returns dau, wau, mau arrays', async () => {
    const result = await metricsApi.getDauWauMau(PROJECT_ID, from, to)
    expect(result.dau).toHaveLength(1)
    expect(result.wau).toHaveLength(1)
    expect(result.mau).toHaveLength(1)
  })

  it('dau point has expected shape', async () => {
    const result = await metricsApi.getDauWauMau(PROJECT_ID, from, to)
    expect(result.dau[0]).toMatchObject({ date: expect.any(String), value: 5 })
  })

  it('passes from/to as query params', async () => {
    let capturedUrl = ''
    server.use(
      http.get(metricsPath('dau-wau-mau'), ({ request }) => {
        capturedUrl = request.url
        return HttpResponse.json(mockDauWauMau)
      }),
    )
    await metricsApi.getDauWauMau(PROJECT_ID, from, to)
    expect(capturedUrl).toContain('from=')
    expect(capturedUrl).toContain('to=')
  })

  it('throws on server error', async () => {
    server.use(
      http.get(metricsPath('dau-wau-mau'), () =>
        HttpResponse.json({ error: 'server error' }, { status: 500 }),
      ),
    )
    await expect(metricsApi.getDauWauMau(PROJECT_ID, from, to)).rejects.toThrow()
  })
})

// ─────────────────────────────────────────────────────────────
//  getRevenue
// ─────────────────────────────────────────────────────────────

describe('metricsApi.getRevenue', () => {
  it('returns revenue response with all fields', async () => {
    const result = await metricsApi.getRevenue(PROJECT_ID, from, to)
    expect(result.totalRevenue).toBe(99.99)
    expect(result.arpu).toBe(4.99)
    expect(result.arppu).toBe(19.99)
    expect(result.payingUsers).toBe(5)
    expect(result.payerConversionRate).toBe(25.0)
  })

  it('dailyRevenue is an array of MetricPoints', async () => {
    const result = await metricsApi.getRevenue(PROJECT_ID, from, to)
    expect(Array.isArray(result.dailyRevenue)).toBe(true)
    result.dailyRevenue.forEach(p => {
      expect(p).toHaveProperty('date')
      expect(p).toHaveProperty('value')
    })
  })

  it('throws on 404', async () => {
    server.use(
      http.get(metricsPath('revenue'), () =>
        HttpResponse.json({ error: 'not found' }, { status: 404 }),
      ),
    )
    await expect(metricsApi.getRevenue(PROJECT_ID, from, to)).rejects.toThrow()
  })
})

// ─────────────────────────────────────────────────────────────
//  getRetention
// ─────────────────────────────────────────────────────────────

describe('metricsApi.getRetention', () => {
  it('returns classic and rolling retention arrays', async () => {
    const result = await metricsApi.getRetention(PROJECT_ID, from, to)
    expect(result.classic).toHaveLength(2)
    expect(result.rolling).toHaveLength(2)
  })

  it('retention points have day and rate', async () => {
    const result = await metricsApi.getRetention(PROJECT_ID, from, to)
    result.classic.forEach(p => {
      expect(typeof p.day).toBe('number')
      expect(typeof p.rate).toBe('number')
    })
  })

  it('cohortTable rows have correct shape', async () => {
    const result = await metricsApi.getRetention(PROJECT_ID, from, to)
    const row    = result.cohortTable[0]
    expect(row.cohortLabel).toBe('Jan 01')
    expect(row.cohortSize).toBe(100)
    expect(Array.isArray(row.retentionRates)).toBe(true)
    // null entries allowed for future periods
    expect(row.retentionRates).toContain(null)
  })

  it('throws on server error', async () => {
    server.use(
      http.get(metricsPath('retention'), () =>
        HttpResponse.json({ error: 'fail' }, { status: 500 }),
      ),
    )
    await expect(metricsApi.getRetention(PROJECT_ID, from, to)).rejects.toThrow()
  })
})

// ─────────────────────────────────────────────────────────────
//  getSessions
// ─────────────────────────────────────────────────────────────

describe('metricsApi.getSessions', () => {
  it('returns session duration and sessions-per-user arrays', async () => {
    const result = await metricsApi.getSessions(PROJECT_ID, from, to)
    expect(result.avgSessionDuration).toHaveLength(1)
    expect(result.sessionsPerUser).toHaveLength(1)
  })

  it('overallAvgDurationSeconds is a number', async () => {
    const result = await metricsApi.getSessions(PROJECT_ID, from, to)
    expect(typeof result.overallAvgDurationSeconds).toBe('number')
    expect(result.overallAvgDurationSeconds).toBe(123.4)
  })

  it('throws on server error', async () => {
    server.use(
      http.get(metricsPath('sessions'), () =>
        HttpResponse.json({ error: 'fail' }, { status: 500 }),
      ),
    )
    await expect(metricsApi.getSessions(PROJECT_ID, from, to)).rejects.toThrow()
  })
})

// ─────────────────────────────────────────────────────────────
//  Helpers
// ─────────────────────────────────────────────────────────────

describe('last30Days', () => {
  it('returns from and to as ISO strings', () => {
    const range = last30Days()
    expect(() => new Date(range.from)).not.toThrow()
    expect(() => new Date(range.to)).not.toThrow()
  })

  it('from is ~30 days before to', () => {
    const range = last30Days()
    const diff  = new Date(range.to).getTime() - new Date(range.from).getTime()
    const days  = diff / (1000 * 60 * 60 * 24)
    expect(days).toBeCloseTo(30, 0)
  })
})

describe('toIso', () => {
  it('converts a Date to ISO string', () => {
    const d   = new Date('2024-01-15T00:00:00Z')
    const iso = toIso(d)
    expect(iso).toBe(d.toISOString())
  })
})
