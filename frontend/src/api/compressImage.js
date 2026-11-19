// Rasmni yuklashdan oldin brauzerda kichraytiradi: eng uzun tomoni MAX_SIDE gacha, WebP (shaffoflik saqlanadi).
// GIF/SVG va allaqachon kichik rasmlar o'zgarmaydi; natija kattaroq chiqsa — asl fayl yuboriladi.
const MAX_SIDE = 1000
const QUALITY = 0.82
const SMALL_BYTES = 150 * 1024

export async function compressImage(file) {
  if (!file?.type?.startsWith('image/') || /gif|svg/.test(file.type)) return file
  let bmp
  try {
    bmp = await createImageBitmap(file)
  } catch {
    return file
  }
  const scale = Math.min(1, MAX_SIDE / Math.max(bmp.width, bmp.height))
  if (scale === 1 && file.size <= SMALL_BYTES) {
    bmp.close()
    return file
  }
  const canvas = document.createElement('canvas')
  canvas.width = Math.round(bmp.width * scale)
  canvas.height = Math.round(bmp.height * scale)
  const ctx = canvas.getContext('2d')
  ctx.imageSmoothingQuality = 'high'
  ctx.drawImage(bmp, 0, 0, canvas.width, canvas.height)
  bmp.close()
  const blob = await new Promise((res) => canvas.toBlob(res, 'image/webp', QUALITY))
  if (!blob || blob.type !== 'image/webp' || blob.size >= file.size) return file
  const name = file.name.replace(/\.[^.]+$/, '') + '.webp'
  return new File([blob], name, { type: 'image/webp' })
}
