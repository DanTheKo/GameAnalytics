// Shared chart colours
export const COLORS = {
  blue:   '#2b7fff',
  teal:   '#14b8a6',
  violet: '#7c3aed',
  amber:  '#f59e0b',
  rose:   '#f43f5e',
  emerald:'#10b981',
  blueFade:   'rgba(43,127,255,0.12)',
  tealFade:   'rgba(20,184,166,0.12)',
  violetFade: 'rgba(124,58,237,0.12)',
  amberFade:  'rgba(245,158,11,0.12)',
}

// Generate labels: last N days
export function dayLabels(n = 30) {
  return Array.from({ length: n }, (_, i) => {
    const d = new Date()
    d.setDate(d.getDate() - (n - 1 - i))
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  })
}

// Generate weekly labels
export function weekLabels(n = 12) {
  return Array.from({ length: n }, (_, i) => `Wk ${i + 1}`)
}

// Smooth random walk
export function randomWalk(n, start, volatility = 0.15, min = 0) {
  const vals = [start]
  for (let i = 1; i < n; i++) {
    const next = vals[i - 1] * (1 + (Math.random() - 0.48) * volatility)
    vals.push(Math.max(min, Math.round(next)))
  }
  return vals
}

export function makeLineDataset(label, data, color, fadedColor) {
  return {
    label,
    data,
    borderColor: color,
    backgroundColor: fadedColor ?? 'transparent',
    borderWidth: 2,
    fill: !!fadedColor,
    tension: 0.4,
    pointRadius: 0,
    pointHoverRadius: 4,
    pointHoverBackgroundColor: color,
    pointHoverBorderColor: '#fff',
    pointHoverBorderWidth: 2,
  }
}

export function makeBarDataset(label, data, color) {
  return {
    label,
    data,
    backgroundColor: color,
    borderRadius: 4,
    borderSkipped: false,
  }
}
