// MXIK kodi yaroqsiz (17 raqam emas, masalan 00000000) mahsulotlar uchun TAXMINIY MXIK TAKLIFI yozadi.
// Faqat Product.MxikSuggestion — chekka tushadigan MxikCode O'ZGARMAYDI.
// Admin panel → Mahsulotlar → "MXIK: taklif bor" filtri → formada "Qo'llash" → saqlash.
//
//   node tools/catalog/mxik-guess.mjs <apiBase> [--apply] [--no-tasnif]
//   POS_USER / POS_PASS — admin login
//
// Tartib: tasnif.soliq.uz (shtrix-kod ANIQ mos) → shu shtrix-kodli boshqa mahsulot → brend+bo'lim →
// brend → ishlab chiqaruvchi (shtrix-kod 7 raqami) → ichki bo'limdagi eng ko'p kod.
import fs from 'node:fs';

const args = process.argv.slice(2);
const apiBase = (args.find((a) => !a.startsWith('--')) || 'http://localhost:5080').replace(/\/$/, '');
const APPLY = args.includes('--apply');
const TASNIF = !args.includes('--no-tasnif');

const valid = (c) => /^\d{17}$/.test(String(c || '').trim());
const norm = (s) => String(s ?? '').trim().toLowerCase();
const brandKey = (name) => (norm(name).match(/[a-z'ʻʼа-яё]{3,}/i) || [])[0];
const ean = (bc) => (/^\d{8}$|^\d{12,14}$/.test(String(bc || '').trim()) ? String(bc).trim() : null);
const makerKey = (bc) => (/^\d{12,14}$/.test(String(bc || '').trim()) ? String(bc).trim().slice(0, 7) : null);

async function api(method, path, token, body) {
  const res = await fetch(apiBase + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
    signal: AbortSignal.timeout(120000),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${text.slice(0, 300)}`);
  return text ? JSON.parse(text) : null;
}
const login = async () => (await api('POST', '/api/auth/login', null, {
  username: process.env.POS_USER || 'admin', password: process.env.POS_PASS || 'Admin12345!',
})).token;

let token = await login();
const products = await api('GET', '/api/products', token);
const categories = await api('GET', '/api/categories', token);
const catName = new Map(categories.map((c) => [c.id, c.name]));
const good = products.filter((p) => valid(p.mxikCode));
const bad = products.filter((p) => !valid(p.mxikCode));
console.log(`mahsulot: ${products.length}, MXIK to'g'ri: ${good.length}, yaroqsiz: ${bad.length}, rejim: ${APPLY ? 'APPLY' : 'faqat hisobot'}`);

// To'g'ri kodli mahsulotlardan "ovozlar".
const tally = {};
const add = (bucket, key, code) => { if (!key) return; const t = ((tally[bucket] ||= {})[key] ||= {}); t[code] = (t[code] || 0) + 1; };
for (const p of good) {
  const code = p.mxikCode.trim();
  const b = brandKey(p.name);
  add('barcode', ean(p.barcode), code);
  add('brandCat', b && `${b}|${p.categoryId}`, code);
  add('brand', b, code);
  add('maker', makerKey(p.barcode), code);
  add('cat', p.categoryId, code);
}
// Eng ko'p uchragan kod (kamida minCount marta).
const top = (bucket, key, minCount = 1) => {
  const t = key && tally[bucket]?.[key];
  if (!t) return null;
  const [code, n] = Object.entries(t).sort((a, b) => b[1] - a[1])[0];
  return n >= minCount ? code : null;
};

async function tasnif(bc) {
  for (let attempt = 0; attempt < 3; attempt++) {
    try {
      const res = await fetch(`https://tasnif.soliq.uz/api/cls-api/elasticsearch/search?search=${bc}&size=5&lang=uz`,
        { headers: { 'User-Agent': 'Mozilla/5.0' }, signal: AbortSignal.timeout(20000) });
      if (!res.ok) { await new Promise((s) => setTimeout(s, 3000)); continue; }
      const d = (await res.json()).data || [];
      const m = d.find((x) => x.internationalCode === bc && valid(x.mxikCode));
      return m ? m.mxikCode : null;
    } catch { await new Promise((s) => setTimeout(s, 3000)); }
  }
  return null;
}

const plan = [];
let n = 0;
for (const p of bad) {
  const bc = ean(p.barcode);
  const b = brandKey(p.name);
  const leafName = catName.get(p.categoryId);
  let code = null, how = null;
  if (TASNIF && bc) { code = await tasnif(bc); how = 'tasnif'; await new Promise((s) => setTimeout(s, 250)); }
  if (!code) { code = top('barcode', bc); how = 'shu shtrix-kod'; }
  if (!code) { code = top('brandCat', b && `${b}|${p.categoryId}`); how = "brend+bo'lim"; }
  if (!code) { code = top('brand', b, 2); how = 'brend'; }
  if (!code) { code = top('maker', makerKey(p.barcode), 2); how = 'ishlab chiqaruvchi'; }
  if (!code && leafName && norm(leafName) !== 'boshqa') { code = top('cat', p.categoryId, 2); how = "bo'lim"; }
  if (!code) how = 'topilmadi';
  plan.push({ p, code, how, leafName });
  if (++n % 200 === 0) console.log(`${n}/${bad.length}`);
}

const byHow = {};
for (const x of plan) byHow[x.how] = (byHow[x.how] || 0) + 1;
console.log('usul:', byHow);
fs.writeFileSync('mxik-guess-report.csv', '﻿' + ["Mahsulot;Shtrix-kod;Hozirgi MXIK;Taklif;Bo'lim;Usul",
  ...plan.map(({ p, code, how, leafName }) => [p.name, p.barcode || '', p.mxikCode || '', code || '', leafName || '', how]
    .map((v) => `"${String(v).replace(/"/g, '""')}"`).join(';'))].join('\n'));
console.log('hisobot: mxik-guess-report.csv');
if (!APPLY) process.exit(0);

token = await login(); // tasnif qidiruvi uzoq davom etgan bo'lishi mumkin
const items = plan.filter((x) => x.code).map((x) => ({ productId: x.p.id, mxikCode: x.code, source: x.how }));
let updated = 0;
for (let i = 0; i < items.length; i += 1000)
  updated += (await api('PUT', '/api/products/bulk-mxik', token, { items: items.slice(i, i + 1000), onlyIfInvalid: true })).updated;
console.log(`MXIK taklifi yozildi: ${updated} (chekka tushadigan MXIK o'zgarmadi)`);
