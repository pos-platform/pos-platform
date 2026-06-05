<script setup>
import { ref, computed, onMounted } from 'vue'
import { inventoriesApi } from '../api/catalog'
import { errorMessage } from '../api/http'

const inventories = ref([])
const loading = ref(false)
const err = ref('')
const success = ref('')
const saving = ref(false)
const expanded = ref(null)
const editingId = ref(null)

// Edited actual quantities keyed by itemId
const editedQtys = ref({})

function fmt(n) { return Number(n || 0).toLocaleString('uz-UZ', { minimumFractionDigits: 0, maximumFractionDigits: 3 }) }
function fmtDate(d) { return new Date(d).toLocaleString('uz-UZ') }
function fmtDisc(n) {
  const v = Number(n || 0)
  if (v === 0) return '—'
  return (v > 0 ? '+' : '') + fmt(v)
}
function discColor(n) {
  const v = Number(n || 0)
  if (v > 0) return 'color:#22c55e'
  if (v < 0) return 'color:#ef4444'
  return 'color:#888'
}

async function load() {
  loading.value = true
  err.value = ''
  try {
    inventories.value = await inventoriesApi.list()
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    loading.value = false
  }
}

async function createInventory() {
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    const inv = await inventoriesApi.create({ notes: null, branchId: null, productIds: null })
    inventories.value.unshift(inv)
    success.value = `Inventarizatsiya ${inv.number} yaratildi — tovarlar miqdorlarini kiriting.`
    expand(inv)
    startEdit(inv)
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

function expand(inv) {
  expanded.value = expanded.value === inv.id ? null : inv.id
}

function startEdit(inv) {
  editingId.value = inv.id
  editedQtys.value = {}
  for (const item of inv.items) {
    editedQtys.value[item.id] = item.actualQty
  }
}

function cancelEdit() {
  editingId.value = null
  editedQtys.value = {}
}

async function saveEdit(inv) {
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    const items = inv.items.map((i) => ({
      itemId: i.id,
      actualQty: Number(editedQtys.value[i.id] ?? i.actualQty)
    }))
    const updated = await inventoriesApi.update(inv.id, { items })
    replaceInList(updated)
    success.value = 'Miqdorlar saqlandi.'
    editingId.value = null
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

async function applyInventory(inv) {
  if (!confirm(`${inv.number} — inventarizatsiyani qo'llash? Tafovutlar omborga yoziladi.`)) return
  saving.value = true
  err.value = ''
  success.value = ''
  try {
    const updated = await inventoriesApi.apply(inv.id)
    replaceInList(updated)
    success.value = `${inv.number} qo'llanildi — ombor yangilandi.`
  } catch (e) {
    err.value = errorMessage(e)
  } finally {
    saving.value = false
  }
}

function replaceInList(updated) {
  const idx = inventories.value.findIndex((i) => i.id === updated.id)
  if (idx !== -1) inventories.value[idx] = updated
}

function expandedInv() {
  return inventories.value.find((i) => i.id === expanded.value)
}

onMounted(load)
</script>

<template>
  <div>
    <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
      <h2 style="margin:0">Inventarizatsiya</h2>
      <button class="btn btn-primary" :disabled="saving" @click="createInventory">
        {{ saving ? 'Yaratilmoqda...' : '+ Yangi inventarizatsiya' }}
      </button>
    </div>

    <div v-if="err" class="alert alert-error" style="margin-bottom:12px">{{ err }}</div>
    <div v-if="success" class="alert alert-success" style="margin-bottom:12px">{{ success }}</div>

    <div v-if="loading" class="muted">Yuklanmoqda...</div>

    <div v-else-if="inventories.length === 0" class="muted">Hech qanday inventarizatsiya yo'q.</div>

    <div v-else class="card" style="padding:0;overflow:hidden">
      <table class="table" style="margin:0">
        <thead>
          <tr>
            <th>Raqam</th>
            <th>Sana</th>
            <th>Holat</th>
            <th>Tovarlar</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="inv in inventories" :key="inv.id">
            <tr style="cursor:pointer" @click="expand(inv)">
              <td><strong>{{ inv.number }}</strong></td>
              <td class="muted">{{ fmtDate(inv.createdAt) }}</td>
              <td>
                <span
                  class="badge"
                  :style="inv.status === 'Completed' ? 'background:#dcfce7;color:#166534' : 'background:#fef9c3;color:#854d0e'"
                >
                  {{ inv.status === 'Completed' ? 'Tugallangan' : 'Qoralama' }}
                </span>
              </td>
              <td class="muted">{{ inv.items.length }} ta</td>
              <td style="text-align:right">
                <span class="muted">{{ expanded === inv.id ? '▲' : '▼' }}</span>
              </td>
            </tr>

            <!-- Detail panel -->
            <tr v-if="expanded === inv.id">
              <td colspan="5" style="padding:0;background:#f8fafc">
                <div style="padding:16px">
                  <!-- Editing mode -->
                  <template v-if="editingId === inv.id">
                    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px">
                      <strong>Haqiqiy miqdorlarni kiriting</strong>
                      <div style="display:flex;gap:8px">
                        <button class="btn btn-ghost btn-sm" :disabled="saving" @click="cancelEdit">Bekor</button>
                        <button class="btn btn-primary btn-sm" :disabled="saving" @click="saveEdit(inv)">
                          {{ saving ? '...' : 'Saqlash' }}
                        </button>
                      </div>
                    </div>
                    <table class="table" style="margin:0">
                      <thead>
                        <tr>
                          <th>Mahsulot</th>
                          <th>Shtrix-kod</th>
                          <th style="text-align:right">Kutilgan</th>
                          <th style="text-align:right">Haqiqiy</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="item in inv.items" :key="item.id">
                          <td>{{ item.productName }}</td>
                          <td class="muted">{{ item.productBarcode || '—' }}</td>
                          <td style="text-align:right">{{ fmt(item.expectedQty) }}</td>
                          <td style="text-align:right;width:120px">
                            <input
                              v-model.number="editedQtys[item.id]"
                              type="number"
                              min="0"
                              step="0.001"
                              class="input"
                              style="width:100%;text-align:right;padding:4px 8px"
                            />
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </template>

                  <!-- View mode -->
                  <template v-else>
                    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px">
                      <strong>Tafovutlar</strong>
                      <div v-if="inv.status === 'Draft'" style="display:flex;gap:8px">
                        <button class="btn btn-ghost btn-sm" @click="startEdit(inv)">Tahrirlash</button>
                        <button class="btn btn-primary btn-sm" :disabled="saving" @click="applyInventory(inv)">
                          Qo'llash
                        </button>
                      </div>
                    </div>
                    <table class="table" style="margin:0">
                      <thead>
                        <tr>
                          <th>Mahsulot</th>
                          <th>Shtrix-kod</th>
                          <th style="text-align:right">Kutilgan</th>
                          <th style="text-align:right">Haqiqiy</th>
                          <th style="text-align:right">Tafovut</th>
                        </tr>
                      </thead>
                      <tbody>
                        <tr v-for="item in inv.items" :key="item.id">
                          <td>{{ item.productName }}</td>
                          <td class="muted">{{ item.productBarcode || '—' }}</td>
                          <td style="text-align:right">{{ fmt(item.expectedQty) }}</td>
                          <td style="text-align:right">{{ fmt(item.actualQty) }}</td>
                          <td style="text-align:right;font-weight:600" :style="discColor(item.discrepancy)">
                            {{ fmtDisc(item.discrepancy) }}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                    <div v-if="inv.notes" style="margin-top:8px" class="muted">Izoh: {{ inv.notes }}</div>
                  </template>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>
