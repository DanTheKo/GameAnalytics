<template>
  <div class="relative group">
    <button
      class="w-9 h-9 flex items-center justify-center rounded-lg transition-all duration-150"
      :class="active
        ? 'bg-brand-50 text-brand-700'
        : 'text-ink-400 hover:bg-surface-100 hover:text-ink-700'"
      @click="$emit('click')"
    >
      <component :is="icon" :size="18" />
    </button>
    <!-- Tooltip -->
    <div class="absolute left-full ml-2 top-1/2 -translate-y-1/2 px-2 py-1 bg-ink-900 text-white
                text-xs rounded whitespace-nowrap opacity-0 pointer-events-none
                group-hover:opacity-100 transition-opacity duration-150 z-50 shadow-float">
      {{ label }}
    </div>
  </div>
</template>

<script setup>
defineProps({ icon: Object, label: String, active: Boolean })
defineEmits(['click'])
</script>
