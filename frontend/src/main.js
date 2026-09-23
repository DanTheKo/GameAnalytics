import { createApp } from 'vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import { createPinia } from 'pinia'
import App from './App.vue'
import './assets/main.css'

import LoginPage from './views/LoginPage.vue'
import ProjectSelectPage from './views/ProjectSelectPage.vue'
import GamePerformance from './views/GamePerformance.vue'
import Retention from './views/Retention.vue'
import Revenue from './views/Revenue.vue'
import UserAcquisition from './views/UserAcquisition.vue'
import Funnels from './views/Funnels.vue'
import EventManager from './views/EventManager.vue'
import EventBrowser from './views/EventBrowser.vue'
import SettingsPage from './views/SettingsPage.vue'
import CustomDashboards from './views/CustomDashboards.vue'

const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/login',    component: LoginPage },
    { path: '/projects', component: ProjectSelectPage },
    { path: '/',         redirect: '/analytics/game-performance' },
    { path: '/analytics/game-performance',   component: GamePerformance },
    { path: '/analytics/retention',          component: Retention },
    { path: '/analytics/revenue',            component: Revenue },
    { path: '/analytics/user-acquisition',   component: UserAcquisition },
    { path: '/analytics/funnels',            component: Funnels },
    { path: '/analytics/event-manager',      component: EventManager },
    { path: '/analytics/event-browser',       component: EventBrowser },
    { path: '/analytics/settings',            component: SettingsPage },
    { path: '/analytics/custom-dashboards',  component: CustomDashboards },
  ],
})

createApp(App).use(createPinia()).use(router).mount('#app')
