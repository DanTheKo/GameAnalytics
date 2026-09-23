<template>
  <div>
    <div class="px-6 pt-5 pb-4 bg-white border-b border-surface-200">
      <h1 class="text-lg font-bold text-ink-900">Settings</h1>
    </div>

    <div class="p-6 max-w-2xl space-y-5">
      <!-- API Key -->
      <div class="stat-card">
        <h2 class="text-sm font-semibold text-ink-900 mb-1">SDK API Key</h2>
        <p class="text-xs text-ink-400 mb-4">Use this key in your game client to send events via the ingestion API.</p>

        <div class="flex items-center gap-2">
          <div class="flex-1 flex items-center gap-2 px-3 py-2 bg-surface-50 border border-surface-200 rounded font-mono text-xs text-ink-700">
            <span class="flex-1 truncate">{{ showKey ? apiKey : maskedKey }}</span>
            <button class="text-ink-400 hover:text-ink-700 transition-colors shrink-0" @click="showKey = !showKey">
              <Eye v-if="!showKey" :size="13" />
              <EyeOff v-else :size="13" />
            </button>
          </div>
          <button class="btn-ghost text-xs" @click="copyKey">
            <Check v-if="copied" :size="13" class="text-emerald-600" />
            <Copy v-else :size="13" />
            {{ copied ? 'Copied' : 'Copy' }}
          </button>
          <button class="btn-ghost text-xs text-red-500 hover:text-red-700 hover:bg-red-50" @click="regenerate" :disabled="regenerating">
            <Loader2 v-if="regenerating" :size="13" class="animate-spin" />
            <RefreshCw v-else :size="13" />
            Regenerate
          </button>
        </div>

        <div v-if="regenerated" class="mt-3 p-3 bg-amber-50 border border-amber-200 rounded text-xs text-amber-700">
          API key regenerated. Update your game client — the old key no longer works.
        </div>

        <div class="mt-4 border-t border-surface-100 pt-4">
          <p class="text-xs font-semibold text-ink-600 mb-2">Usage example</p>
          <pre class="text-[11px] font-mono bg-surface-50 border border-surface-200 rounded p-3 text-ink-600 overflow-x-auto">POST http://your-api/api/ingest/events
X-Api-Key: {{ showKey ? apiKey : maskedKey }}
Content-Type: application/json

{
  "events": [{
    "eventName": "level_complete",
    "eventTimestamp": "{{ exampleTs }}",
    "eventData": "{\"level_id\": 5}",
    "sessionId": "...",
    "userId": "..."
  }]
}</pre>
        </div>
      </div>

      <!-- Project info -->
      <div class="stat-card">
        <h2 class="text-sm font-semibold text-ink-900 mb-4">Project</h2>
        <div class="space-y-3">
          <div>
            <label class="block text-xs font-medium text-ink-600 mb-1">Name</label>
            <input v-model="projectName" type="text"
              class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
          </div>
          <div>
            <label class="block text-xs font-medium text-ink-600 mb-1">Description</label>
            <input v-model="projectDesc" type="text"
              class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
          </div>
          <div>
            <label class="block text-xs font-medium text-ink-600 mb-1">Project ID</label>
            <div class="flex items-center gap-2 px-3 py-2 bg-surface-50 border border-surface-200 rounded font-mono text-xs text-ink-400">
              {{ projectStore.activeProjectId }}
            </div>
          </div>
        </div>
        <div class="flex items-center gap-2 mt-4">
          <button class="btn-primary text-xs" :disabled="savingProject" @click="saveProject">
            <Loader2 v-if="savingProject" :size="13" class="animate-spin" />
            Save changes
          </button>
          <p v-if="savedProject" class="text-xs text-emerald-600">Saved!</p>
        </div>
      </div>

      <!-- Account -->
      <div class="stat-card">
        <h2 class="text-sm font-semibold text-ink-900 mb-4">Account</h2>
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-ink-900">{{ auth.username }}</p>
            <p class="text-xs text-ink-400 mt-0.5">System administrator</p>
          </div>
          <button class="btn-ghost text-xs text-red-500 hover:text-red-700 hover:bg-red-50" @click="logout">
            <LogOut :size="13" /> Sign out
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Eye, EyeOff, Copy, Check, RefreshCw, Loader2, LogOut } from 'lucide-vue-next'
import { useAuthStore, useProjectStore } from '../stores/index'
import { projectsApi } from '../api/index'
import { api } from '../api/client'

const auth = useAuthStore()
const projectStore = useProjectStore()
const router = useRouter()

const showKey = ref(false)
const copied = ref(false)
const regenerating = ref(false)
const regenerated = ref(false)
const savingProject = ref(false)
const savedProject = ref(false)

const apiKey = ref(projectStore.activeProject?.apiKey ?? '')
const projectName = ref(projectStore.activeProject?.name ?? '')
const projectDesc = ref(projectStore.activeProject?.description ?? '')

const maskedKey = computed(() => apiKey.value ? apiKey.value.slice(0, 6) + '••••••••••••••••••••••••••' : '')
const exampleTs = new Date().toISOString()

async function copyKey() {
  await navigator.clipboard.writeText(apiKey.value)
  copied.value = true
  setTimeout(() => copied.value = false, 2000)
}

async function regenerate() {
  if (!confirm('Regenerate the API key? The old key will stop working immediately.')) return
  regenerating.value = true
  regenerated.value = false
  try {
    const res = await api.post(`/api/projects/${projectStore.activeProjectId}/regenerate-api-key`, {})
    apiKey.value = res.apiKey
    regenerated.value = true
  } finally {
    regenerating.value = false
  }
}

async function saveProject() {
  savingProject.value = true
  savedProject.value = false
  try {
    await projectsApi.update(projectStore.activeProjectId, {
      name: projectName.value,
      description: projectDesc.value,
    })
    await projectStore.fetchProjects(auth.userId)
    savedProject.value = true
    setTimeout(() => savedProject.value = false, 2000)
  } finally {
    savingProject.value = false
  }
}

function logout() {
  auth.logout()
  router.push('/login')
}

onMounted(() => {
  apiKey.value = projectStore.activeProject?.apiKey ?? ''
  projectName.value = projectStore.activeProject?.name ?? ''
  projectDesc.value = projectStore.activeProject?.description ?? ''
})
</script>
