<template>
  <header class="h-12 flex items-center justify-between px-4 bg-white border-b border-surface-200 shrink-0 z-20">
    <div class="flex items-center gap-2 w-[220px]">
      <!--<div class="w-7 h-7 rounded-lg bg-gradient-to-br from-brand-500 to-brand-700 flex items-center justify-center shadow-sm">
         <Zap :size="14" class="text-white" /> 
      </div>-->
      <span class="text-sm font-bold text-ink-900 tracking-tight">Game Analytics</span>
    </div>

    <div class="flex items-center gap-2 text-sm">
      <span class="text-ink-300">/</span>
      <div class="relative">
        <button
          class="flex items-center gap-1.5 text-ink-900 font-semibold hover:text-brand-700 transition-colors"
          @click="open = !open"
        >
          <BarChart2 :size="13" />
          {{ projectStore.activeProject?.name ?? '—' }}
          <ChevronDown :size="11" class="text-ink-400" />
        </button>

        <div v-if="open" class="absolute top-full left-0 mt-1 w-56 bg-white border border-surface-200 rounded-lg shadow-float z-50 py-1">
          <button
            v-for="p in projectStore.projects" :key="p.id"
            class="w-full flex items-center justify-between px-3 py-2 text-sm hover:bg-surface-50 transition-colors"
            @click="selectProject(p.id)"
          >
            <span :class="p.id === projectStore.activeProjectId ? 'font-semibold text-brand-700' : 'text-ink-700'">{{ p.name }}</span>
            <Check v-if="p.id === projectStore.activeProjectId" :size="13" class="text-brand-500" />
          </button>
          <div class="border-t border-surface-100 mt-1 pt-1">
            <button class="w-full px-3 py-2 text-sm text-ink-400 hover:text-ink-700 hover:bg-surface-50 text-left transition-colors" @click="goProjects">
              Manage projects
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="flex items-center gap-1 w-[220px] justify-end">
      <button class="btn-ghost text-ink-400 hover:text-ink-700 p-2" @click="router.push('/analytics/settings')">
        <Settings :size="16" />
      </button>
      <div class="w-px h-5 bg-surface-200 mx-1"></div>
      <div class="flex items-center gap-2 px-2 py-1.5">
        <button data-testid="gamification-badge" class="w-6 h-6 rounded-full bg-gradient-to-br from-brand-400 to-brand-600 flex items-center justify-center text-white text-xs font-bold" @click="isProfileOpen = true">
          {{ auth.username?.[0]?.toUpperCase() }}
          </button>
        <span class="text-xs font-medium text-ink-700">{{ auth.username }}</span>
      </div>
      <button class="btn-ghost text-xs text-ink-400 p-2" @click="logout">
        <LogOut :size="15" />
      </button>
    </div>

  </header>
<!--  <div v-if="open" class="fixed inset-0 z-40" @click="open = false" />-->
  <!-- <ProfileOverlay v-model="isProfileOpen" /> -->
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Zap, BarChart2, ChevronDown, Settings, LogOut, Check } from 'lucide-vue-next'
import { useAuthStore, useProjectStore } from '../stores/index'
//import ProfileOverlay from '../gamification/components/gamification/ProfileOverlay.vue'

//import { useGamificationStore } from '../gamification/stores/gamification'

const auth = useAuthStore()
const projectStore = useProjectStore()
const router = useRouter()
const open = ref(false)

const isProfileOpen = ref(false)

//const gamification = useGamificationStore()

onMounted(() => {
  if (auth.userId && !projectStore.projects?.length) {
    projectStore.fetchProjects(auth.userId)
  }
})

function selectProject(id) {
  projectStore.setActiveProject(id)
  open.value = false
}

function goProjects() {
  open.value = false
  router.push('/projects')
}

function logout() {
  auth.logout()
  router.push('/login')
}

function checkProfile() {
  // gamification.loadProfile(useAuthStore.userId, useAuthStore.username)
  // gamification.loadLeaderboard('xp')
}
</script>
