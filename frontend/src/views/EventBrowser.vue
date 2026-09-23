<template>
  <div>
    <div class="px-6 pt-5 pb-4 bg-white border-b border-surface-200">
      <h1 class="text-lg font-bold text-ink-900 mb-3">Event Browser</h1>
      <div class="flex items-center gap-2 flex-wrap">
        <select
          v-model="filters.eventModelId"
          class="chip border-0 bg-white border border-surface-200 pr-6 text-xs font-medium text-ink-700 focus:outline-none focus:border-brand-400 cursor-pointer"
          @change="load(true)"
        >
          <option value="">All events</option>
          <option v-for="m in eventModels" :key="m.id" :value="m.id">{{ m.name }}</option>
        </select>

        <input type="date" v-model="filters.from"
          class="chip text-xs font-mono focus:outline-none focus:border-brand-400"
          @change="load(true)" />
        <span class="text-ink-300 text-xs">—</span>
        <input type="date" v-model="filters.to"
          class="chip text-xs font-mono focus:outline-none focus:border-brand-400"
          @change="load(true)" />

        <button class="btn-ghost text-xs ml-auto" @click="load(true)">
          <RefreshCw :size="13" /> Refresh
        </button>
      </div>
    </div>

    <div class="p-6">
      <div class="stat-card p-0 overflow-hidden">
        <div v-if="loading" class="flex items-center justify-center py-16 text-ink-300">
          <Loader2 :size="20" class="animate-spin" />
        </div>

        <div v-else-if="!events.length" class="flex flex-col items-center py-16 text-ink-300">
          <Database :size="28" class="mb-2" />
          <p class="text-sm">No events found</p>
        </div>

        <table v-else class="w-full text-xs">
          <thead>
            <tr class="border-b border-surface-100">
              <th class="text-left text-ink-400 font-semibold px-4 py-2.5">Timestamp</th>
              <th class="text-left text-ink-400 font-semibold px-4 py-2.5">Event</th>
              <th class="text-left text-ink-400 font-semibold px-4 py-2.5">User</th>
              <th class="text-left text-ink-400 font-semibold px-4 py-2.5">Session</th>
              <th class="text-left text-ink-400 font-semibold px-4 py-2.5">Data</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="e in events" :key="e.id"
              class="border-b border-surface-50 hover:bg-surface-50 transition-colors cursor-pointer"
              @click="selected = selected?.id === e.id ? null : e"
            >
              <td class="px-4 py-2.5 font-mono text-ink-500 whitespace-nowrap">
                {{ formatTs(e.eventTimestamp) }}
              </td>
              <td class="px-4 py-2.5">
                <span v-if="e.eventModelName" class="px-2 py-0.5 bg-brand-50 text-brand-700 rounded font-medium">
                  {{ e.eventModelName }}
                </span>
                <span v-else class="text-ink-300">unknown</span>
              </td>
              <td class="px-4 py-2.5 font-mono text-ink-400">
                {{ e.userId ? e.userId.slice(0, 8) + '…' : '—' }}
              </td>
              <td class="px-4 py-2.5 font-mono text-ink-400">
                {{ e.sessionId ? e.sessionId.slice(0, 8) + '…' : '—' }}
              </td>
              <td class="px-4 py-2.5 text-ink-500 max-w-xs truncate font-mono">
                {{ e.eventData }}
              </td>
            </tr>
          </tbody>
        </table>

        <!-- Expanded event data -->
        <div v-if="selected" class="border-t border-surface-200 bg-surface-50 px-4 py-3">
          <div class="flex items-center justify-between mb-2">
            <p class="text-xs font-semibold text-ink-600">Event Data</p>
            <button class="text-ink-300 hover:text-ink-600" @click="selected = null"><X :size="13" /></button>
          </div>
          <pre class="text-[11px] font-mono text-ink-700 whitespace-pre-wrap">{{ formatJson(selected.eventData) }}</pre>
        </div>

        <!-- Pagination -->
        <div v-if="total > pageSize" class="flex items-center justify-between px-4 py-3 border-t border-surface-100">
          <p class="text-xs text-ink-400">{{ total }} total events</p>
          <div class="flex items-center gap-1">
            <button class="btn-ghost text-xs p-1.5" :disabled="page === 1" @click="changePage(page - 1)">
              <ChevronLeft :size="14" />
            </button>
            <span class="text-xs text-ink-600 px-2">{{ page }} / {{ totalPages }}</span>
            <button class="btn-ghost text-xs p-1.5" :disabled="page >= totalPages" @click="changePage(page + 1)">
              <ChevronRight :size="14" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { RefreshCw, Loader2, Database, X, ChevronLeft, ChevronRight } from 'lucide-vue-next'
import { eventsApi } from '../api/index'
import { useEventModelsStore, useProjectStore } from '../stores/index'

const projectStore = useProjectStore()
const eventModelsStore = useEventModelsStore()

const events = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = 15
const loading = ref(false)
const selected = ref(null)

const now = new Date()
const monthAgo = new Date(); monthAgo.setDate(now.getDate() - 30)

const filters = ref({
  eventModelId: '',
  from: monthAgo.toISOString().slice(0, 10),
  to: now.toISOString().slice(0, 10),
})

const eventModels = computed(() => eventModelsStore.eventModels)
const totalPages = computed(() => Math.ceil(total.value / pageSize))

function formatTs(iso) {
  return new Date(iso).toLocaleString('en-GB', {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function formatJson(str) {
  try { return JSON.stringify(JSON.parse(str), null, 2) }
  catch { return str }
}

function changePage(p) {
  page.value = p
  load(false)
}

async function load(resetPage = false) {
  if (!projectStore.activeProjectId) return
  if (resetPage) page.value = 1
  loading.value = true
  selected.value = null
  try {
    const res = await eventsApi.browse(projectStore.activeProjectId, {
      eventModelId: filters.value.eventModelId || undefined,
      from: new Date(filters.value.from).toISOString(),
      to:   new Date(filters.value.to + 'T23:59:59').toISOString(),
      page: page.value,
      pageSize,
    })
    events.value = res.items
    total.value  = res.total
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  const pid = projectStore.activeProjectId
  if (pid) {
    await eventModelsStore.fetchEventModels(pid)
    await load(true)
  }
})

watch(() => projectStore.activeProjectId, async (pid) => {
  if (pid) {
    await eventModelsStore.fetchEventModels(pid)
    await load(true)
  }
})
</script>
