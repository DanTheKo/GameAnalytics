<template>
  <div
    v-if="show"
    class="fixed bottom-4 right-4 z-50 flex items-center gap-3 bg-amber-50 border border-amber-200 rounded-lg shadow-float px-4 py-3 text-xs"
  >
    <Clock :size="14" class="text-amber-500 shrink-0" />
    <span class="text-amber-700">Session expires in <strong>{{ minutesLeft }} min</strong></span>
    <button class="btn-primary text-xs py-1 px-2" @click="refresh">Stay logged in</button>
    <button class="text-amber-400 hover:text-amber-600" @click="show = false"><X :size="13" /></button>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { Clock, X } from 'lucide-vue-next'
import { useAuthStore } from '../stores/index'
import { useRouter } from 'vue-router'

const auth   = useAuthStore()
const router = useRouter()
const show   = ref(false)

const minutesLeft = computed(() =>
  Math.ceil(auth.tokenExpiresIn / 1000 / 60)
)

let timer

onMounted(() => {
  timer = setInterval(() => {
    const mins = minutesLeft.value
    if (mins <= 0) {
      auth.logout()
      router.push('/login?expired=1')
    } else if (mins <= 15) {
      show.value = true
    }
  }, 30_000)
})

onUnmounted(() => clearInterval(timer))

async function refresh() {
  try {
    await auth.login(auth.username || '', '')
  } catch {
    show.value = false
  }
}
</script>
