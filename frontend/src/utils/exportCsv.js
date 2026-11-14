import { downloadBlob } from '../api/catalog'

// Jadvalni Excel ochadigan CSV ga yuklab beradi (UTF-8 BOM, ";" ajratgich — uz/ru Excel'da ustunlarga to'g'ri bo'linadi).
// columns: [{ title, value: (row) => any }]
export function exportCsv(filename, columns, rows) {
  const esc = (v) => {
    if (v === null || v === undefined) return ''
    const s = String(v)
    return /[";\n\r]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
  }
  const lines = [columns.map((c) => esc(c.title)).join(';')]
  for (const r of rows) lines.push(columns.map((c) => esc(c.value(r))).join(';'))
  const blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' })
  downloadBlob(blob, filename)
}
