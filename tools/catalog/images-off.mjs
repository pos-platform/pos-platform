// Rasmi yo'q, haqiqiy shtrix-kodli mahsulotlarga Open Food Facts'dan rasm qo'yadi.
//
//   node tools/catalog/images-off.mjs <apiBase> [--apply] [--limit=N]
//   POS_USER / POS_PASS — admin login (standart: admin / Admin12345!)
//
// --apply'siz faqat qidiradi va images-off-report.csv yozadi.
// --apply: rasmni yuklab, POST /api/products/image orqali serverga joylaydi va
// PUT /api/products/bulk-images bilan faqat rasmi bo'sh mahsulotlarga yozadi (boshqa maydonlarga tegmaydi).
// Rasmlar Open Food Facts'dan (CC BY-SA) — ilovada manba ko'rsatilishi tavsiya etiladi.
import fs from 'node:fs';

const args = process.argv.slice(2);
const apiBase = (args.find((a) => !a.startsWith('--')) || 'http://localhost:5080').replace(/\/$/, '');
const APPLY = args.includes('--apply');
const LIMIT = Number(args.find((a) => a.startsWith('--limit='))?.split('=')[1]) || Infinity;
const UA = 'pos-platform-image-lookup/1.0';
const CONCURRENCY = 3; // OFF: ~100 so'rov/daqiqa chegarasi

async function api(method, path, token, body, raw) {
  const res = await fetch(apiBase + path, {
    method,
    headers: { ...(raw ? {} : { 'Content-Type': 'application/json' }), ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: raw ?? (body === undefined ? undefined : JSON.stringify(body)),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${text.slice(0, 200)}`);
  return text ? JSON.parse(text) : null;
}

const login = async () => (await api('POST', '/api/auth/login', null, {
  username: process.env.POS_USER || 'admin', password: process.env.POS_PASS || 'Admin12345!',
})).token;
let token = await login();
const products = await api('GET', '/api/products', token);
// Tizim yaratgan ichki kodlar (04...) va tarozi kodlari (2...) — xalqaro bazada yo'q.
const isEan = (b) => /^\d{8}$|^\d{12,14}$/.test(b) && !b.startsWith('04') && !b.startsWith('2');
const targets = products.filter((p) => !p.imageUrl && isEan((p.barcode || '').trim())).slice(0, LIMIT);
console.log(`mahsulot: ${products.length}, tekshiriladi: ${targets.length}, rejim: ${APPLY ? 'APPLY' : 'faqat qidiruv'}`);

const found = [];
let i = 0, done = 0;
async function lookup(bc) {
  for (let attempt = 0; attempt < 3; attempt++) {
    const res = await fetch(`https://world.openfoodfacts.org/api/v2/product/${bc}.json?fields=product_name,brands,image_front_url,image_url`,
      { headers: { 'User-Agent': UA }, signal: AbortSignal.timeout(20000) });
    if (res.status === 404) return null;
    if (res.status === 429 || res.status >= 500) { await new Promise((s) => setTimeout(s, 5000 * (attempt + 1))); continue; }
    const j = await res.json();
    const img = j.product?.image_front_url || j.product?.image_url;
    return j.status === 1 && img ? { img, offName: [j.product.brands, j.product.product_name].filter(Boolean).join(' · ') } : null;
  }
  return null;
}
async function worker() {
  while (i < targets.length) {
    const p = targets[i++];
    try {
      const r = await lookup(p.barcode.trim());
      if (r) found.push({ p, ...r });
    } catch { /* tarmoq xatosi — o'tkazib yuboramiz */ }
    if (++done % 50 === 0) console.log(`${done}/${targets.length}, topildi: ${found.length}`);
  }
}
await Promise.all(Array.from({ length: CONCURRENCY }, worker));
console.log(`topildi: ${found.length} / ${targets.length}`);
fs.writeFileSync('images-off-report.csv', '﻿' + ['Mahsulot;Shtrix-kod;OFF nomi;Rasm',
  ...found.map(({ p, offName, img }) => [p.name, p.barcode, offName, img].map((v) => `"${String(v).replace(/"/g, '""')}"`).join(';'))].join('\n'));
if (!APPLY || found.length === 0) process.exit(0);

// Yuklab olish → serverga joylash → bulk-images.
token = await login(); // qidiruv soatlab davom etadi — token eskirgan bo'lishi mumkin
const items = [];
for (const { p, img } of found) {
  try {
    const res = await fetch(img, { headers: { 'User-Agent': UA }, signal: AbortSignal.timeout(20000) });
    if (!res.ok) continue;
    const blob = await res.blob();
    const ext = img.toLowerCase().endsWith('.png') ? 'png' : 'jpg';
    const fd = new FormData();
    fd.append('file', blob, `${p.barcode.trim()}.${ext}`);
    const { url } = await api('POST', '/api/products/image', token, undefined, fd);
    items.push({ productId: p.id, imageUrl: url });
  } catch (e) { console.log(`o'tkazildi ${p.name}: ${e.message}`); }
}
let updated = 0;
for (let k = 0; k < items.length; k += 1000)
  updated += (await api('PUT', '/api/products/bulk-images', token, { items: items.slice(k, k + 1000), onlyIfEmpty: true })).updated;
console.log(`rasm qo'yildi: ${updated}`);
