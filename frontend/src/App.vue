<template>
  <div class="flex flex-col h-screen overflow-hidden bg-surface-50">
    <template v-if="auth.isLoggedIn && projectStore.activeProjectId">
      <AppHeader />
      <div class="flex flex-1 overflow-hidden">
        <MainSidebar />
        <LocalSidebar :section="activeSection" />
        <main class="flex-1 overflow-y-auto">
          <router-view v-slot="{ Component }">
            <transition name="page" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </main>
      </div>
      <TokenExpiryWarning />
    </template>
    <router-view v-else />
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import MainSidebar from './components/MainSidebar.vue'
import LocalSidebar from './components/LocalSidebar.vue'
import TokenExpiryWarning from './components/TokenExpiryWarning.vue'
import { useAuthStore } from './stores/index'
import { useProjectStore } from './stores/index'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const projectStore = useProjectStore()
const activeSection = ref('analytics')

watch(() => route.path, path => {
  if (path.startsWith('/analytics')) activeSection.value = 'analytics'
})

watch([() => auth.isLoggedIn, () => projectStore.activeProjectId], ([loggedIn, pid]) => {
  if (!loggedIn) { router.push('/login'); return }
  if (!pid)      { router.push('/projects'); return }
}, { immediate: true })
</script>

<style>
.page-enter-active, .page-leave-active { transition: opacity 0.2s ease, transform 0.2s ease; }
.page-enter-from  { opacity: 0; transform: translateX(6px); }
.page-leave-to    { opacity: 0; transform: translateX(-6px); }
</style>
