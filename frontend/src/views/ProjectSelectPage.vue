<template>
  <div class="min-h-screen bg-surface-50 flex items-center justify-center p-4">
    <div class="w-full max-w-lg">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-lg font-bold text-ink-900">Select a project</h1>
          <p class="text-xs text-ink-400 mt-0.5">Logged in as <span class="font-medium text-ink-600">{{ auth.username }}</span></p>
        </div>
        <button class="btn-ghost text-xs text-ink-400" @click="logout">Sign out</button>
      </div>

      <div v-if="projectStore.loading" class="space-y-2">
        <div v-for="n in 2" :key="n" class="h-16 bg-white rounded-lg border border-surface-200 animate-pulse" />
      </div>

      <div v-else class="space-y-2">
        <button
          v-for="p in projectStore.projects" :key="p.id"
          class="w-full flex items-center justify-between bg-white border border-surface-200 rounded-lg px-4 py-3 hover:border-brand-300 hover:bg-brand-50 transition-all text-left"
          @click="select(p.id)"
        >
          <div>
            <p class="text-sm font-semibold text-ink-900">{{ p.name }}</p>
            <p class="text-xs text-ink-400 mt-0.5">{{ p.description }}</p>
          </div>
          <ChevronRight :size="16" class="text-ink-300 shrink-0" />
        </button>

        <div v-if="!projectStore.projects.length" class="text-center py-8 text-ink-300 text-sm">
          No projects yet
        </div>
      </div>

      <div class="mt-4 border-t border-surface-100 pt-4">
        <div v-if="!showForm" class="flex justify-center">
          <button class="btn-ghost text-sm" @click="showForm = true">
            <Plus :size="14" /> New project
          </button>
        </div>

        <div v-else class="space-y-3">
          <input v-model="newName" type="text" placeholder="Project name"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
          <input v-model="newDesc" type="text" placeholder="Description (optional)"
            class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
          <div class="flex gap-2">
            <button class="btn-primary text-xs" :disabled="!newName || creating" @click="create">
              <Loader2 v-if="creating" :size="13" class="animate-spin" /> Create
            </button>
            <button class="btn-ghost text-xs" @click="showForm = false; newName = ''; newDesc = ''">Cancel</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ChevronRight, Plus, Loader2 } from 'lucide-vue-next'
import { useAuthStore, useProjectStore } from '../stores/index'

const router = useRouter()
const auth = useAuthStore()
const projectStore = useProjectStore()

const showForm = ref(false)
const newName = ref('')
const newDesc = ref('')
const creating = ref(false)

onMounted(() => {
  if (auth.userId) projectStore.fetchProjects(auth.userId)
})

function select(id) {
  projectStore.setActiveProject(id)
  router.push('/')
}

async function create() {
  creating.value = true
  try {
    const p = await projectStore.createProject(auth.userId, newName.value, newDesc.value)
    select(p.id)
  } finally {
    creating.value = false
  }
}

function logout() {
  auth.logout()
  router.push('/login')
}
</script>
