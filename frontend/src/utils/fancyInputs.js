// Global: native <select> va sana/vaqt inputlarini chiroyli dropdown/kalendar bilan almashtiradi.
// Native element DOMda yashirin qoladi (v-model, @change, required ishlayveradi) —
// yonida trigger-tugma chiziladi. Native elementga data-native qo'yilsa — o'zgartirilmaydi.

const L = {
  uz: {
    months: ['Yanvar', 'Fevral', 'Mart', 'Aprel', 'May', 'Iyun', 'Iyul', 'Avgust', 'Sentabr', 'Oktabr', 'Noyabr', 'Dekabr'],
    short: ['yan', 'fev', 'mar', 'apr', 'may', 'iyn', 'iyl', 'avg', 'sen', 'okt', 'noy', 'dek'],
    wd: ['Du', 'Se', 'Ch', 'Pa', 'Ju', 'Sh', 'Ya'],
    today: 'Bugun', clear: 'Tozalash', done: 'Tayyor', pickDate: 'Sanani tanlang', pickTime: 'Vaqtni tanlang',
    search: 'Qidirish...', empty: 'Hech narsa topilmadi', hour: 'Soat', minute: 'Daqiqa', now: 'Hozir'
  },
  ru: {
    months: ['Январь', 'Февраль', 'Март', 'Апрель', 'Май', 'Июнь', 'Июль', 'Август', 'Сентябрь', 'Октябрь', 'Ноябрь', 'Декабрь'],
    short: ['янв', 'фев', 'мар', 'апр', 'мая', 'июн', 'июл', 'авг', 'сен', 'окт', 'ноя', 'дек'],
    wd: ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'],
    today: 'Сегодня', clear: 'Очистить', done: 'Готово', pickDate: 'Выберите дату', pickTime: 'Выберите время',
    search: 'Поиск...', empty: 'Ничего не найдено', hour: 'Час', minute: 'Минута', now: 'Сейчас'
  }
}
const t = () => L[(localStorage.getItem('lang') || 'uz') === 'ru' ? 'ru' : 'uz']

const ICON_CHEV = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m6 9 6 6 6-6"/></svg>'
const ICON_CAL = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4.5" width="18" height="17" rx="3"/><path d="M3 9.5h18M8 2.5v4M16 2.5v4"/></svg>'
const ICON_CLOCK = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg>'
const ICON_X = '<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round"><path d="M6 6l12 12M18 6 6 18"/></svg>'
const ICON_CHECK = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round"><path d="m5 12 5 5 9-10"/></svg>'
const ICON_L = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m15 6-6 6 6 6"/></svg>'
const ICON_R = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m9 6 6 6-6 6"/></svg>'

const DATE_TYPES = ['date', 'datetime-local', 'time']
const esc = (s) => String(s ?? '').replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]))
const pad = (n) => String(n).padStart(2, '0')

// ---------- umumiy: trigger yaratish, xususiyatlarni ko'chirish ----------
function copyLook(src, dst, extraClass) {
  // Vue class'ni qayta yozsa — fx-native qaytariladi (contains tekshiruvi: cheksiz mutatsiya bo'lmasin)
  if (!src.classList.contains('fx-native')) src.classList.add('fx-native')
  const keep = ['fx-open', 'fx-invalid', 'fx-has'].filter((c) => dst.classList.contains(c)).join(' ')
  dst.className = (src.getAttribute('class') || '').replace(/\bfx-native\b/g, '') + ' ' + extraClass + ' ' + keep
  const st = src.getAttribute('style')
  if (st) dst.setAttribute('style', st)
  else dst.removeAttribute('style')
  for (const a of src.attributes) if (a.name.startsWith('data-v-')) dst.setAttribute(a.name, '')
  if (src.title) dst.title = src.title
  dst.disabled = src.disabled || src.readOnly
}

function hookValue(el, proto, props, cb) {
  for (const p of props) {
    const d = Object.getOwnPropertyDescriptor(proto, p)
    if (!d) continue
    Object.defineProperty(el, p, {
      configurable: true,
      get() { return d.get.call(this) },
      set(v) { d.set.call(this, v); cb() }
    })
  }
}

function linkNative(el, btn) {
  el.classList.add('fx-native')
  el.addEventListener('focus', () => btn.focus())
  el.addEventListener('invalid', () => btn.classList.add('fx-invalid'))
  el.addEventListener('change', () => btn.classList.remove('fx-invalid'))
}

function fire(el) {
  el.dispatchEvent(new Event('input', { bubbles: true }))
  el.dispatchEvent(new Event('change', { bubbles: true }))
}

// ---------- popup boshqaruvi (bitta vaqtning o'zida bittasi) ----------
let current = null // { pop, trigger, close }

function closePopup() {
  if (!current) return
  const c = current
  current = null
  c.trigger.classList.remove('fx-open')
  c.pop.classList.add('fx-leave')
  setTimeout(() => c.pop.remove(), 120)
  c.onClose?.()
}

function openPopup(trigger, pop, onClose) {
  closePopup()
  document.body.appendChild(pop)
  current = { pop, trigger, onClose }
  trigger.classList.add('fx-open')
  place()
}

function place() {
  if (!current) return
  const { pop, trigger } = current
  if (!trigger.isConnected) return closePopup()
  const r = trigger.getBoundingClientRect()
  const vw = window.innerWidth, vh = window.innerHeight
  pop.style.minWidth = pop.dataset.fixedw ? '' : r.width + 'px'
  const ph = pop.offsetHeight, pw = pop.offsetWidth
  const below = vh - r.bottom, above = r.top
  let top = below >= ph + 8 || below >= above ? r.bottom + 6 : r.top - ph - 6
  top = Math.max(8, Math.min(top, vh - ph - 8))
  let left = Math.min(r.left, vw - pw - 8)
  left = Math.max(8, left)
  pop.style.top = top + 'px'
  pop.style.left = left + 'px'
  pop.classList.toggle('fx-up', top < r.top)
}

document.addEventListener('mousedown', (e) => {
  if (!current) return
  if (current.pop.contains(e.target) || current.trigger.contains(e.target)) return
  closePopup()
}, true)
window.addEventListener('resize', place)
document.addEventListener('scroll', (e) => {
  if (current && !current.pop.contains(e.target)) place()
}, true)
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape' && current) {
    const tr = current.trigger
    closePopup()
    tr.focus()
    e.stopPropagation()
  }
}, true)

// ================= SELECT =================
function enhanceSelect(sel) {
  if (sel._fx || sel.multiple || sel.size > 1 || sel.hasAttribute('data-native')) return
  const btn = document.createElement('button')
  btn.type = 'button'
  btn.innerHTML = '<span class="fx-label"></span><span class="fx-chev">' + ICON_CHEV + '</span>'
  const label = btn.firstChild

  const refresh = () => {
    if (!sel.isConnected) return
    copyLook(sel, btn, 'fx-trigger fx-select')
    const o = sel.options[sel.selectedIndex]
    const text = o ? o.text : ''
    label.textContent = text || ' '
    label.classList.toggle('fx-ph', !o || o.value === '')
    if (current?.trigger === btn) current.rerender?.()
  }

  sel._fx = { btn, refresh }
  linkNative(sel, btn)
  sel.tabIndex = -1
  sel.after(btn)
  hookValue(sel, HTMLSelectElement.prototype, ['value', 'selectedIndex'], () => queueMicrotask(refresh))
  new MutationObserver(refresh).observe(sel, { childList: true, subtree: true, attributes: true, characterData: true })
  refresh()

  btn.addEventListener('click', () => (current?.trigger === btn ? closePopup() : openSelect(sel, btn)))
  btn.addEventListener('keydown', (e) => {
    if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(e.key) && current?.trigger !== btn) {
      e.preventDefault()
      openSelect(sel, btn)
    }
  })
}

function openSelect(sel, btn) {
  const pop = document.createElement('div')
  pop.className = 'fx-pop fx-list-pop'
  const many = sel.options.length > 8
  pop.innerHTML = (many ? `<div class="fx-search"><input type="text" placeholder="${esc(t().search)}" /></div>` : '') + '<div class="fx-list" role="listbox"></div>'
  const list = pop.querySelector('.fx-list')
  const search = pop.querySelector('.fx-search input')
  let q = ''
  let active = sel.selectedIndex

  const visible = () => [...sel.options].map((o, i) => ({ o, i })).filter(({ o }) => !o.hidden && (!q || o.text.toLowerCase().includes(q)))

  const render = () => {
    const items = visible()
    if (!items.some((x) => x.i === active)) active = items.find((x) => !x.o.disabled)?.i ?? -1
    list.innerHTML = items.length
      ? items.map(({ o, i }) => {
          const g = o.parentElement?.tagName === 'OPTGROUP' && o === o.parentElement.querySelector('option') ? `<div class="fx-group">${esc(o.parentElement.label)}</div>` : ''
          const cls = ['fx-opt', i === sel.selectedIndex && 'fx-sel', i === active && 'fx-act', o.disabled && 'fx-dis', o.value === '' && 'fx-optph'].filter(Boolean).join(' ')
          return `${g}<div class="${cls}" data-i="${i}" role="option"><span class="fx-opt-t">${esc(o.text) || '&nbsp;'}</span><span class="fx-tick">${ICON_CHECK}</span></div>`
        }).join('')
      : `<div class="fx-empty">${esc(t().empty)}</div>`
    list.querySelector('.fx-act')?.scrollIntoView({ block: 'nearest' })
  }

  const choose = (i) => {
    const o = sel.options[i]
    if (!o || o.disabled) return
    closePopup()
    if (i !== sel.selectedIndex) {
      Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'selectedIndex').set.call(sel, i)
      fire(sel)
      sel._fx.refresh()
    }
    btn.focus()
  }

  list.addEventListener('mousedown', (e) => e.preventDefault())
  list.addEventListener('click', (e) => {
    const el = e.target.closest('.fx-opt')
    if (el) choose(+el.dataset.i)
  })
  list.addEventListener('mousemove', (e) => {
    const el = e.target.closest('.fx-opt')
    if (el && +el.dataset.i !== active) {
      list.querySelector('.fx-act')?.classList.remove('fx-act')
      el.classList.add('fx-act')
      active = +el.dataset.i
    }
  })
  const onKey = (e) => {
    const items = visible().filter((x) => !x.o.disabled)
    const pos = items.findIndex((x) => x.i === active)
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault()
      const n = e.key === 'ArrowDown' ? Math.min(items.length - 1, pos + 1) : Math.max(0, pos - 1)
      if (items[n]) { active = items[n].i; render() }
    } else if (e.key === 'Enter') {
      e.preventDefault()
      if (active >= 0) choose(active)
    } else if (e.key === 'Tab') closePopup()
  }
  pop.addEventListener('keydown', onKey)
  btn.addEventListener('keydown', onKey)
  if (search) search.addEventListener('input', () => { q = search.value.trim().toLowerCase(); render(); place() })

  openPopup(btn, pop, () => btn.removeEventListener('keydown', onKey))
  current.rerender = render
  render()
  place()
  if (search) search.focus()
}

// ================= SANA / VAQT =================
function parseVal(type, v) {
  if (!v) return null
  if (type === 'time') {
    const [h, m] = v.split(':').map(Number)
    return { h: h || 0, mi: m || 0 }
  }
  const [d, tm = ''] = v.split('T')
  const [y, mo, da] = d.split('-').map(Number)
  if (!y) return null
  const [h, mi] = tm.split(':').map(Number)
  return { y, mo: mo - 1, d: da, h: h || 0, mi: mi || 0 }
}
function fmtVal(type, s) {
  const date = `${s.y}-${pad(s.mo + 1)}-${pad(s.d)}`
  const time = `${pad(s.h)}:${pad(s.mi)}`
  return type === 'date' ? date : type === 'time' ? time : `${date}T${time}`
}
function display(type, v) {
  const s = parseVal(type, v)
  if (!s) return ''
  const time = `${pad(s.h)}:${pad(s.mi)}`
  if (type === 'time') return time
  const date = `${pad(s.d)} ${t().short[s.mo]} ${s.y}`
  return type === 'date' ? date : `${date}, ${time}`
}
const ymd = (y, m, d) => `${y}-${pad(m + 1)}-${pad(d)}`

function enhanceDate(inp) {
  if (inp._fx || inp.hasAttribute('data-native') || !DATE_TYPES.includes(inp.type)) return
  const type = inp.type
  const btn = document.createElement('button')
  btn.type = 'button'
  btn.innerHTML = `<span class="fx-ico">${type === 'time' ? ICON_CLOCK : ICON_CAL}</span><span class="fx-label"></span><span class="fx-clear" title="${esc(t().clear)}">${ICON_X}</span>`
  const label = btn.querySelector('.fx-label')
  const clr = btn.querySelector('.fx-clear')

  const refresh = () => {
    if (!inp.isConnected) return
    copyLook(inp, btn, 'fx-trigger fx-date')
    const txt = display(type, inp.value)
    label.textContent = txt || (type === 'time' ? t().pickTime : t().pickDate)
    label.classList.toggle('fx-ph', !txt)
    btn.classList.toggle('fx-has', !!txt && !inp.required && !btn.disabled)
    if (current?.trigger === btn) current.rerender?.()
  }

  inp._fx = { btn, refresh }
  linkNative(inp, btn)
  inp.tabIndex = -1
  inp.after(btn)
  hookValue(inp, HTMLInputElement.prototype, ['value'], () => queueMicrotask(refresh))
  new MutationObserver(refresh).observe(inp, { attributes: true })
  refresh()

  clr.addEventListener('click', (e) => {
    e.stopPropagation()
    setVal(inp, '')
    closePopup()
  })
  btn.addEventListener('click', () => (current?.trigger === btn ? closePopup() : openDate(inp, btn)))
  btn.addEventListener('keydown', (e) => {
    if (['ArrowDown', 'Enter', ' '].includes(e.key) && current?.trigger !== btn) { e.preventDefault(); openDate(inp, btn) }
  })
}

function setVal(inp, v) {
  Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(inp, v)
  fire(inp)
  inp._fx.refresh()
}

function openDate(inp, btn) {
  const type = inp.type
  const hasDate = type !== 'time'
  const hasTime = type !== 'date'
  const now = new Date()
  const cur = parseVal(type, inp.value)
  const st = cur ? { ...cur } : { y: now.getFullYear(), mo: now.getMonth(), d: null, h: now.getHours(), mi: 0 }
  let view = { y: st.y ?? now.getFullYear(), m: st.mo ?? now.getMonth() }
  let mode = 'days' // days | months
  const minD = (inp.min || '').slice(0, 10)
  const maxD = (inp.max || '').slice(0, 10)
  const todayStr = ymd(now.getFullYear(), now.getMonth(), now.getDate())

  const pop = document.createElement('div')
  pop.className = 'fx-pop fx-cal-pop' + (hasTime && hasDate ? ' fx-wide' : '') + (!hasDate ? ' fx-time-only' : '')
  pop.dataset.fixedw = '1'
  pop.tabIndex = -1

  const commit = () => {
    if (hasDate && st.d == null) return
    setVal(inp, fmtVal(type, st))
  }

  const daysHtml = () => {
    const first = new Date(view.y, view.m, 1)
    const offset = (first.getDay() + 6) % 7
    const dim = new Date(view.y, view.m + 1, 0).getDate()
    const prevDim = new Date(view.y, view.m, 0).getDate()
    const selStr = st.d != null ? ymd(st.y, st.mo, st.d) : ''
    let cells = ''
    for (let i = 0; i < 42; i++) {
      let y = view.y, m = view.m, d = i - offset + 1, out = false
      if (d < 1) { m -= 1; d = prevDim + d; out = true }
      else if (d > dim) { m += 1; d = d - dim; out = true }
      const dt = new Date(y, m, d)
      const s = ymd(dt.getFullYear(), dt.getMonth(), dt.getDate())
      const dis = (minD && s < minD) || (maxD && s > maxD)
      const cls = ['fx-day', out && 'fx-out', s === todayStr && 'fx-today', s === selStr && 'fx-sel', dis && 'fx-dis', i % 7 >= 5 && 'fx-we'].filter(Boolean).join(' ')
      cells += `<button type="button" class="${cls}" data-s="${s}" ${dis ? 'disabled' : ''}>${dt.getDate()}</button>`
    }
    return `<div class="fx-wd">${t().wd.map((w, i) => `<span class="${i >= 5 ? 'fx-we' : ''}">${w}</span>`).join('')}</div><div class="fx-days">${cells}</div>`
  }

  const monthsHtml = () =>
    `<div class="fx-months">${t().months.map((n, i) => `<button type="button" class="fx-mon${i === view.m ? ' fx-sel' : ''}${i === now.getMonth() && view.y === now.getFullYear() ? ' fx-today' : ''}" data-m="${i}">${n.slice(0, 3)}</button>`).join('')}</div>`

  const timeHtml = () => {
    const col = (n, step, val, k) => {
      let h = ''
      for (let i = 0; i < n; i += step) h += `<button type="button" class="fx-tc${i === val ? ' fx-sel' : ''}" data-${k}="${i}">${pad(i)}</button>`
      return h
    }
    return `<div class="fx-time">
      <div class="fx-time-head"><span>${t().hour}</span><span>${t().minute}</span></div>
      <div class="fx-time-cols"><div class="fx-col" data-col="h">${col(24, 1, st.h, 'h')}</div><div class="fx-col" data-col="mi">${col(60, 1, st.mi, 'mi')}</div></div>
    </div>`
  }

  const render = () => {
    let html = ''
    if (hasDate) {
      html += `<div class="fx-cal">
        <div class="fx-head">
          <button type="button" class="fx-nav" data-nav="-1">${ICON_L}</button>
          <button type="button" class="fx-title">${mode === 'days' ? `${t().months[view.m]} ${view.y}` : view.y}</button>
          <button type="button" class="fx-nav" data-nav="1">${ICON_R}</button>
        </div>
        ${mode === 'days' ? daysHtml() : monthsHtml()}
      </div>`
    }
    if (hasTime) html += timeHtml()
    html = `<div class="fx-body">${html}</div><div class="fx-foot">
      <button type="button" class="fx-link" data-act="today">${hasDate ? t().today : t().now}</button>
      <span class="fx-sp"></span>
      ${!inp.required ? `<button type="button" class="fx-link fx-muted" data-act="clear">${t().clear}</button>` : ''}
      ${hasTime ? `<button type="button" class="fx-done" data-act="done">${t().done}</button>` : ''}
    </div>`
    pop.innerHTML = html
    pop.querySelectorAll('.fx-col').forEach((c) => {
      const s = c.querySelector('.fx-sel')
      if (s) c.scrollTop = s.offsetTop - c.clientHeight / 2 + s.offsetHeight / 2
    })
  }

  pop.addEventListener('mousedown', (e) => { if (e.target.closest('button')) e.preventDefault() })
  pop.addEventListener('click', (e) => {
    const b = e.target.closest('button')
    if (!b || b.disabled) return
    if (b.dataset.nav) {
      const n = +b.dataset.nav
      if (mode === 'days') {
        view.m += n
        if (view.m < 0) { view.m = 11; view.y-- } else if (view.m > 11) { view.m = 0; view.y++ }
      } else view.y += n
      render()
    } else if (b.classList.contains('fx-title')) {
      mode = mode === 'days' ? 'months' : 'days'
      render()
    } else if (b.dataset.m != null) {
      view.m = +b.dataset.m
      mode = 'days'
      render()
    } else if (b.dataset.s) {
      const [y, m, d] = b.dataset.s.split('-').map(Number)
      Object.assign(st, { y, mo: m - 1, d })
      view = { y, m: m - 1 }
      commit()
      if (!hasTime) { closePopup(); btn.focus() } else render()
    } else if (b.dataset.h != null || b.dataset.mi != null) {
      if (b.dataset.h != null) st.h = +b.dataset.h
      else st.mi = +b.dataset.mi
      const noDate = hasDate && st.d == null
      if (noDate) Object.assign(st, { y: now.getFullYear(), mo: now.getMonth(), d: now.getDate() })
      commit()
      if (noDate) { view = { y: st.y, m: st.mo }; render() } else {
        b.parentElement.querySelectorAll('.fx-sel').forEach((x) => x.classList.remove('fx-sel'))
        b.classList.add('fx-sel')
      }
    } else if (b.dataset.act === 'today') {
      Object.assign(st, { y: now.getFullYear(), mo: now.getMonth(), d: now.getDate() })
      if (hasTime) { st.h = now.getHours(); st.mi = now.getMinutes() }
      view = { y: st.y, m: st.mo }
      mode = 'days'
      commit()
      if (!hasTime) { closePopup(); btn.focus() } else render()
    } else if (b.dataset.act === 'clear') {
      setVal(inp, '')
      closePopup()
      btn.focus()
    } else if (b.dataset.act === 'done') {
      if (!inp.value) commit()
      closePopup()
      btn.focus()
    }
  })

  openPopup(btn, pop)
  render()
  place()
}

// ================= ISHGA TUSHIRISH =================
function scan(root) {
  if (root.nodeType !== 1) return
  if (root.matches?.('select')) enhanceSelect(root)
  else if (root.matches?.('input')) enhanceDate(root)
  root.querySelectorAll?.('select, input[type=date], input[type=datetime-local], input[type=time]').forEach((el) =>
    el.tagName === 'SELECT' ? enhanceSelect(el) : enhanceDate(el)
  )
}

function cleanup(node) {
  if (node.nodeType !== 1) return
  const list = node._fx ? [node] : [...(node.querySelectorAll?.('.fx-native') || [])]
  for (const el of list) {
    if (el._fx && !el.isConnected) {
      el._fx.btn.remove()
      if (current?.trigger === el._fx.btn) closePopup()
    }
  }
}

export function installFancyInputs() {
  scan(document.body)
  new MutationObserver((muts) => {
    for (const m of muts) {
      m.addedNodes.forEach(scan)
      m.removedNodes.forEach(cleanup)
    }
  }).observe(document.body, { childList: true, subtree: true })
}
