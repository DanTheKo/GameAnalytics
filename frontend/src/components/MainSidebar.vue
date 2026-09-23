<template>
  <aside class="w-[56px] flex flex-col items-center py-3 gap-1 bg-white border-r border-surface-200 shrink-0 z-10">
    <NavIcon :icon="BarChart2" label="Analytics" :active="true" />
  </aside>
</template>

<script setup>
import { BarChart2 } from 'lucide-vue-next'
import NavIcon from './NavIcon.vue'
</script>
