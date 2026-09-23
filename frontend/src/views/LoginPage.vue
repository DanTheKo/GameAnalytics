<template>
  <div class="min-h-screen bg-surface-50 flex items-center justify-center">
    <div class="w-full max-w-sm">
      <div class="flex items-center justify-center gap-2 mb-8">
        <!--<div class="w-8 h-8 rounded-lg bg-gradient-to-br from-brand-500 to-brand-700 flex items-center justify-center shadow-sm">
           <Zap :size="16" class="text-white" />
        </div>-->
        <span class="text-lg font-bold text-ink-900">Game Analytics</span>
      </div>

      <div class="bg-white rounded-lg shadow-card border border-surface-200 p-6">
        <!-- First run banner -->
        <div v-if="isSetup" class="mb-5 p-3 bg-brand-50 border border-brand-200 rounded-lg">
          <p class="text-xs font-semibold text-brand-700 mb-0.5">First time setup</p>
          <p class="text-xs text-brand-600">No accounts exist yet. Create your admin account to get started.</p>
        </div>

        <!-- Session expired banner -->
        <div v-if="sessionExpired" class="mb-5 p-3 bg-amber-50 border border-amber-200 rounded-lg">
          <p class="text-xs text-amber-700">Your session expired. Please log in again.</p>
        </div>

        <template v-if="!isSetup">
          <div class="flex gap-1 mb-5 bg-surface-100 rounded p-0.5">
            <button
              v-for="tab in ['Login', 'Register']" :key="tab"
              class="flex-1 py-1.5 text-sm font-medium rounded transition-all"
              :class="mode === tab ? 'bg-white text-ink-900 shadow-sm' : 'text-ink-400 hover:text-ink-700'"
              @click="mode = tab; error = ''"
            >{{ tab }}</button>
          </div>
        </template>

        <div class="space-y-3">
          <div>
            <label class="block text-xs font-medium text-ink-600 mb-1">Username</label>
            <input v-model="username" type="text" placeholder="admin" autofocus
              class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all"
              @keydown.enter="submit" />
          </div>
          <div>
            <label class="block text-xs font-medium text-ink-600 mb-1">Password</label>
            <input v-model="password" type="password" placeholder="••••••••"
              class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all"
              @keydown.enter="submit" />
          </div>
          <div v-if="isSetup || mode === 'Register'">
            <label class="block text-xs font-medium text-ink-600 mb-1">Confirm password</label>
            <input v-model="confirm" type="password" placeholder="••••••••"
              class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all"
              @keydown.enter="submit" />
          </div>
        </div>

        <p v-if="error" class="mt-3 text-xs text-red-500">{{ error }}</p>

        <button class="btn-primary w-full justify-center mt-4" :disabled="loading" @click="submit">
          <Loader2 v-if="loading" :size="14" class="animate-spin" />
          {{ isSetup ? 'Create admin account' : mode }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { Zap, Loader2 } from 'lucide-vue-next'
import { useAuthStore } from '../stores/index'

const router  = useRouter()
const route   = useRoute()
const auth    = useAuthStore()

const mode     = ref('Login')
const username = ref('')
const password = ref('')
const confirm  = ref('')
const loading  = ref(false)
const error    = ref('')
const isSetup  = ref(false)

const sessionExpired = route.query.expired === '1'

onMounted(async () => {
  try { isSetup.value = await auth.checkNeedsSetup() }
  catch {}
})

async function submit() {
  error.value = ''
  if (!username.value || !password.value) { error.value = 'All fields are required.'; return }

  if ((isSetup.value || mode.value === 'Register') && password.value !== confirm.value) {
    error.value = 'Passwords do not match.'
    return
  }

  loading.value = true
  try {
    if (isSetup.value) {
      await auth.setup(username.value, password.value)
    } else if (mode.value === 'Login') {
      await auth.login(username.value, password.value)
    } else {
      await auth.setup(username.value, password.value)
    }
    router.push('/projects')
  } catch (e) {
    error.value = e.message ?? 'Something went wrong'
  } finally {
    loading.value = false
  }
}
</script>
