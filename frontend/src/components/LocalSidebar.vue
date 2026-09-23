<template>
  <aside class="w-[200px] flex flex-col bg-white border-r border-surface-200 shrink-0 overflow-y-auto">
    <!-- Section header -->
    <div class="px-4 pt-4 pb-2">
      <p class="text-[10px] font-semibold uppercase tracking-widest text-ink-300">{{ config.title }}</p>
    </div>

    <nav class="flex-1 px-2 space-y-0.5">
      <template v-for="group in config.groups" :key="group.label">
        <!-- Group label -->
        <p v-if="group.label" class="text-[10px] font-semibold uppercase tracking-widest text-ink-300 px-2 pt-4 pb-1">
          {{ group.label }}
        </p>

        <RouterLink
          v-for="item in group.items"
          :key="item.path"
          :to="item.path"
          class="nav-item"
          active-class="active"
        >
          <component :is="item.icon" :size="15" class="shrink-0" />
          {{ item.label }}
        </RouterLink>
      </template>
    </nav>


  </aside>
</template>

<script setup>
import { computed } from 'vue'
import {
  LayoutDashboard, TrendingUp, DollarSign, Users,
  BarChart2, Settings, BellRing, Crosshair, Database
} from 'lucide-vue-next'

const props = defineProps({ section: String })

const configs = {
  analytics: {
    title: 'Gaming Services · Analytics',
    groups: [
      {
        label: 'Analyze',
        items: [
          { path: '/analytics/game-performance', label: 'Game performance',  icon: BarChart2 },
          { path: '/analytics/retention',         label: 'Retention',         icon: TrendingUp },
          { path: '/analytics/revenue',            label: 'Revenue',           icon: DollarSign },
          { path: '/analytics/user-acquisition',  label: 'User acquisition',  icon: Users },
          //{ path: '/analytics/funnels',            label: 'Funnels',           icon: Crosshair },
        ],
      },
      {
        label: 'Manage',
        items: [
          { path: '/analytics/event-manager',     label: 'Event Manager',     icon: BellRing },
          { path: '/analytics/event-browser',      label: 'Event Browser',     icon: Database },
          { path: '/analytics/custom-dashboards',  label: 'Custom Dashboards', icon: LayoutDashboard },
          { path: '/analytics/settings',           label: 'Settings',          icon: Settings },
        ],
      },
    ],
    bottomLinks: [],
  },
  dashboard: {
    title: 'Dashboard',
    groups: [{ items: [{ path: '/', label: 'Overview', icon: LayoutDashboard }] }],
    bottomLinks: [],
  },
}

const config = computed(() => configs[props.section] ?? configs.analytics)
</script>
