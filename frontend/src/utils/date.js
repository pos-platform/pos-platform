// Sana yordamchilari. Biznes kuni — foydalanuvchining mahalliy (Toshkent) kuni.
// toISOString() UTC beradi: 00:00–05:00 oralig'ida "bugun" kechagi kun bo'lib qoladi — ishlatmang.

/** Date → mahalliy YYYY-MM-DD. */
export function localDateStr(d = new Date()) {
  const off = d.getTimezoneOffset()
  return new Date(d.getTime() - off * 60000).toISOString().slice(0, 10)
}

/** Bugundan n kun oldin/keyin (manfiy — oldin) mahalliy YYYY-MM-DD. */
export function localDateShift(days) {
  const d = new Date()
  d.setDate(d.getDate() + days)
  return localDateStr(d)
}
