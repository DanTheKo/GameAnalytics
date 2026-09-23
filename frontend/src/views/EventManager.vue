<template>
  <div>
    <div class="px-6 pt-5 pb-4 bg-white border-b border-surface-200">
      <div class="flex items-center justify-between">
        <h1 class="text-lg font-bold text-ink-900">Event Manager</h1>
        <button class="btn-primary text-xs" @click="openCreate">
          <Plus :size="13" /> New Event
        </button>
      </div>
    </div>

    <div class="p-6 space-y-3">
      <div v-if="store.loading" class="space-y-2">
        <div v-for="n in 3" :key="n" class="h-16 bg-white rounded-lg border border-surface-200 animate-pulse" />
      </div>

      <div v-else-if="!store.eventModels.length" class="stat-card flex flex-col items-center py-12 text-ink-300">
        <BellRing :size="28" class="mb-2" />
        <p class="text-sm">No event models yet</p>
      </div>

      <div v-else v-for="model in store.eventModels" :key="model.id" class="stat-card">
        <div class="flex items-start justify-between">
          <div class="flex items-start gap-3">
            <button
              class="mt-0.5 w-8 h-5 rounded-full transition-colors relative shrink-0"
              :class="model.isEnabled ? 'bg-brand-500' : 'bg-surface-200'"
              @click="store.toggleEnabled(projectId, model.id)"
            >
              <span
                class="absolute top-0.5 w-4 h-4 bg-white rounded-full shadow-sm transition-all"
                :class="model.isEnabled ? 'left-3.5' : 'left-0.5'"
              />
            </button>
            <div>
              <div class="flex items-center gap-2">
                <p class="text-sm font-semibold text-ink-900">{{ model.name }}</p>
                <span class="px-1.5 py-0.5 text-[10px] font-medium rounded" :class="typeClass(model.eventType)">
                  {{ model.eventType }}
                </span>
              </div>
              <p class="text-xs text-ink-400 mt-0.5">{{ model.description || '—' }}</p>
            </div>
          </div>
          <div class="flex items-center gap-1">
            <button class="btn-ghost p-1.5 text-ink-400" @click="toggleParams(model.id)">
              <ChevronDown :size="14" :class="expanded === model.id ? 'rotate-180' : ''" class="transition-transform" />
            </button>
            <button class="btn-ghost p-1.5 text-red-400 hover:text-red-600 hover:bg-red-50" @click="deleteModel(model)">
              <Trash2 :size="14" />
            </button>
          </div>
        </div>

        <div v-if="expanded === model.id" class="mt-4 border-t border-surface-100 pt-4">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-semibold text-ink-400 uppercase tracking-widest">Parameters</p>
            <button class="btn-ghost text-xs" @click="openAddParam(model.id)">
              <Plus :size="12" /> Add
            </button>
          </div>

          <div v-if="!model.parameters.length" class="text-xs text-ink-300 py-2">No parameters defined</div>

          <div v-else class="space-y-1">
            <div v-for="p in model.parameters" :key="p.id"
              class="flex items-center justify-between px-3 py-2 bg-surface-50 rounded text-xs">
              <div class="flex items-center gap-3">
                <span class="font-medium text-ink-900">{{ p.name }}</span>
                <span class="text-ink-400">{{ p.parameterType }}</span>
                <span class="text-ink-400">{{ p.dataType }}</span>
              </div>
              <button class="text-ink-300 hover:text-red-500 transition-colors" @click="deleteParam(model.id, p.id)">
                <X :size="12" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="modal" class="fixed inset-0 bg-black/20 z-50 flex items-center justify-center p-4" @click.self="modal = null">
      <div class="bg-white rounded-lg shadow-float w-full max-w-sm p-5">
        <h2 class="text-sm font-bold text-ink-900 mb-4">{{ modal === 'create' ? 'New Event Model' : 'Add Parameter' }}</h2>

        <template v-if="modal === 'create'">
          <div class="space-y-3">
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Name</label>
              <input v-model="form.name" type="text" placeholder="e.g. level_complete"
                class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Description</label>
              <input v-model="form.description" type="text" placeholder="Optional"
                class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Event Type</label>
              <select v-model="form.eventType"
                class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option v-for="t in eventTypes" :key="t" :value="t">{{ t }}</option>
              </select>
            </div>
          </div>
        </template>

        <template v-if="modal === 'param'">
          <div class="space-y-3">
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Name</label>
              <input v-model="form.name" type="text" placeholder="e.g. amount"
                class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 focus:ring-2 focus:ring-brand-100 transition-all" />
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Parameter Type</label>
              <select v-model="form.parameterType"
                class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option value="EventParameter">EventParameter</option>
                <option value="UserProperty">UserProperty</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-ink-600 mb-1">Data Type</label>
              <select v-model="form.dataType"
                class="w-full px-3 py-2 text-sm border border-surface-200 rounded focus:outline-none focus:border-brand-400 transition-all">
                <option v-for="t in dataTypes" :key="t" :value="t">{{ t }}</option>
              </select>
            </div>
          </div>
        </template>

        <p v-if="formError" class="mt-2 text-xs text-red-500">{{ formError }}</p>

        <div class="flex gap-2 mt-4">
          <button class="btn-primary text-xs" :disabled="saving" @click="submitForm">
            <Loader2 v-if="saving" :size="13" class="animate-spin" /> Save
          </button>
          <button class="btn-ghost text-xs" @click="modal = null">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { Plus, Trash2, ChevronDown, X, BellRing, Loader2 } from 'lucide-vue-next'
import { useEventModelsStore, useProjectStore } from '../stores/index'

const store = useEventModelsStore()
const projectStore = useProjectStore()
const projectId = ref(projectStore.activeProjectId)

const expanded = ref(null)
const modal = ref(null)
const saving = ref(false)
const formError = ref('')
const activeModelId = ref(null)

const form = ref({ name: '', description: '', eventType: 'Custom', parameterType: 'EventParameter', dataType: 'String' })

const eventTypes = ['Custom', 'Session', 'Revenue', 'Error', 'Progression']
const dataTypes  = ['String', 'Int', 'Float', 'Bool', 'DateTime']

const typeClass = (t) => ({
  Revenue:     'bg-emerald-50 text-emerald-700',
  Error:       'bg-red-50 text-red-600',
  Session:     'bg-blue-50 text-blue-700',
  Progression: 'bg-violet-50 text-violet-700',
  Custom:      'bg-surface-100 text-ink-500',
}[t] ?? 'bg-surface-100 text-ink-500')

function toggleParams(id) { expanded.value = expanded.value === id ? null : id }

function openCreate() {
  form.value = { name: '', description: '', eventType: 'Custom' }
  formError.value = ''
  modal.value = 'create'
}

function openAddParam(modelId) {
  activeModelId.value = modelId
  form.value = { name: '', parameterType: 'EventParameter', dataType: 'String' }
  formError.value = ''
  modal.value = 'param'
}

async function submitForm() {
  if (!form.value.name) { formError.value = 'Name is required'; return }
  saving.value = true
  formError.value = ''
  try {
    if (modal.value === 'create') {
      await store.createEventModel(projectId.value, { ...form.value, isEnabled: true })
    } else {
      await store.addParameter(projectId.value, activeModelId.value, form.value)
    }
    modal.value = null
  } catch (e) {
    formError.value = e.message
  } finally {
    saving.value = false
  }
}

async function deleteModel(model) {
  if (!confirm(`Delete "${model.name}"?`)) return
  await store.deleteEventModel(projectId.value, model.id)
}

async function deleteParam(modelId, paramId) {
  await store.deleteParameter(projectId.value, modelId, paramId)
}

async function load() {
  projectId.value = projectStore.activeProjectId
  if (projectId.value) await store.fetchEventModels(projectId.value)
}

onMounted(load)
watch(() => projectStore.activeProjectId, load)
</script>
