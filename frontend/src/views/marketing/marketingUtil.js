// Bosqich 9 (CONTRACT-9): "Ilova reklamasi" bo'limi uchun umumiy yordamchilar.

// Banner / story slaydi / push havola turi (backend enum string).
export const LINK_TYPES = [
  { value: 'None', text: 'Havolasiz' },
  { value: 'Product', text: 'Mahsulot' },
  { value: 'Category', text: 'Kategoriya' },
  { value: 'Collection', text: 'Kolleksiya' },
  { value: 'Discounts', text: 'Aksiyalar sahifasi' },
  { value: 'Url', text: 'Tashqi havola (https)' }
]

// <input type="datetime-local"> qiymati ↔ UTC ISO.
export function toLocalInput(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return ''
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 16)
}
export function fromLocalInput(v) {
  if (!v) return null
  const d = new Date(v)
  return Number.isNaN(d.getTime()) ? null : d.toISOString()
}

export function fmtDateTime(v) {
  if (!v) return ''
  return new Date(v).toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
export const fmtNum = (n) => Math.round(Number(n) || 0).toLocaleString('ru-RU')

export function periodText(startsAt, endsAt) {
  if (!startsAt && !endsAt) return 'Muddatsiz'
  return `${fmtDateTime(startsAt) || '…'} – ${fmtDateTime(endsAt) || '…'}`
}

// Hozir ilovada ko'rinadimi: faol va muddat ichida.
export function stateOf(x) {
  if (!x.isActive) return { text: "O'chiq", cls: 'pill-gray' }
  const now = new Date()
  if (x.endsAt && new Date(x.endsAt) < now) return { text: 'Muddati tugagan', cls: 'pill-red' }
  if (x.startsAt && new Date(x.startsAt) > now) return { text: 'Rejada', cls: 'pill-amber' }
  return { text: 'Ko\'rinmoqda', cls: 'pill-green' }
}

// Havola matni (jadvalda).
export function linkText(linkType, linkValue, { products = [], categories = [], collections = [] } = {}) {
  const t = LINK_TYPES.find((l) => l.value === linkType)
  if (!linkType || linkType === 'None') return '—'
  if (linkType === 'Discounts') return t.text
  const find = (arr) => arr.find((x) => x.id === linkValue)
  if (linkType === 'Product') return `Mahsulot: ${find(products)?.name || linkValue || '?'}`
  if (linkType === 'Category') return `Kategoriya: ${find(categories)?.name || linkValue || '?'}`
  if (linkType === 'Collection') return `Kolleksiya: ${find(collections)?.title || linkValue || '?'}`
  if (linkType === 'Url') return linkValue || '?'
  return t?.text || linkType
}

// Havola qiymatini tekshirish → xato matni yoki ''.
export function validateLink(linkType, linkValue) {
  const v = (linkValue || '').trim()
  if (['Product', 'Category', 'Collection'].includes(linkType) && !v) {
    return `Havola uchun ${LINK_TYPES.find((l) => l.value === linkType).text.toLowerCase()} tanlang.`
  }
  if (linkType === 'Url' && !/^https:\/\/\S+$/i.test(v)) return 'Havola https:// bilan boshlanishi kerak.'
  return ''
}

// Havola qiymati body uchun: None/Discounts — null.
export function linkValueFor(linkType, linkValue) {
  if (!linkType || linkType === 'None' || linkType === 'Discounts') return null
  return (linkValue || '').trim() || null
}

// Massivda elementni bir pog'ona siljitish (yangi massiv).
export function moved(arr, index, dir) {
  const j = index + dir
  if (j < 0 || j >= arr.length) return arr
  const copy = [...arr]
  ;[copy[index], copy[j]] = [copy[j], copy[index]]
  return copy
}
