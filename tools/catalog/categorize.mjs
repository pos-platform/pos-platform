// Mahsulotlarni MXIK guruhi bo'yicha ikki darajali bo'limlarga avtomatik ajratadi.
//
//   node tools/catalog/categorize.mjs <apiBase> [--apply]
//   POS_USER / POS_PASS — admin login (standart: admin / Admin12345!)
//
// --apply'siz faqat hisobot (categorize-report.csv) — bazaga hech narsa yozmaydi.
// Tartib: qo'lda qo'yilgan ma'noli eski kategoriya → MXIK (birinchi 5 raqam) → nomdagi kalit so'z → "Boshqa".
// Faqat kategoriya o'zgaradi (PUT /api/products/bulk-category) — qoldiq, narx va boshqa maydonlarga tegmaydi.
import fs from 'node:fs';

const args = process.argv.slice(2);
const apiBase = (args.find((a) => !a.startsWith('--')) || 'http://localhost:5080').replace(/\/$/, '');
const APPLY = args.includes('--apply');

// [asosiy bo'lim, onlayn?, [[ichki bo'lim, [MXIK prefikslari]], ...]]
const TREE = [
  ['Meva va sabzavotlar', true, [
    ['Sabzavotlar', ['00701', '00702', '00703', '00704', '00706', '00707', '00709', '00710', '00799']],
    ['Mevalar', ['00803', '00804', '00805', '00807', '00809', '00810']],
    ["Quruq mevalar va yong'oqlar", ['00801', '00806', '00808', '00811', '01207']],
  ]],
  ['Sut mahsulotlari va tuxum', true, [
    ['Sut va kefir', ['00401', '00402']],
    ['Yogurt va desertlar', ['00403']],
    ["Sariyog' va qaymoq", ['00405']],
    ['Pishloq va tvorog', ['00406', '09406', '10408']],
    ['Tuxum', ['00407']],
  ]],
  ["Go'sht, baliq, kolbasa", true, [
    ["Go'sht va parranda", ['00201', '00207']],
    ['Baliq va ikra', ['00302', '00303', '00305']],
    ['Kolbasa va sosiskalar', ['01601']],
  ]],
  ['Non va pishiriqlar', true, [
    ['Non', []],
    ['Pechenye, keks va rulet', ['01905']],
  ]],
  ['Shirinliklar', true, [
    ['Shokolad', ['01806']],
    ['Konfetlar', ['01704']],
    ['Marmelad va jem', ['02007']],
    ['Asal', ['00409', '01702']],
  ]],
  ['Bakaleya', true, [
    ['Guruch va yormalar', ['01006', '01103', '01104', '01904']],
    ['Un', ['01101', '01102']],
    ['Makaron va tez tayyor', ['01902', '05601']],
    ["O'simlik yog'i", ['01507', '01508', '01509', '01512', '01517']],
    ['Shakar va tuz', ['01701', '02501']],
    ['Ziravorlar va qo\'shimchalar', ['00904', '00909', '00910', '02918', '02106', '02102']],
  ]],
  ['Konserva va souslar', true, [
    ['Souslar va ketchup', ['02103']],
    ['Sabzavot konservalari', ['02001', '02002', '00708', '00711']],
    ["Go'sht va baliq konservalari", ['01602', '01604']],
    ['Sirka', ['02209', '02915']],
  ]],
  ['Choy va qahva', true, [
    ['Choy', ['00902', '07419']],
    ['Qahva', ['00901', '02101']],
  ]],
  ['Ichimliklar', true, [
    ['Suv', ['02201', '02853']],
    ['Gazli ichimliklar', ['02202', '02206']],
    ['Sharbatlar', ['02009', '08435']],
  ]],
  ['Muzqaymoq va muzlatilgan', true, [
    ['Muzqaymoq', ['02105']],
    ['Muzlatilgan va tayyor ovqat', ['10202', '02302']],
  ]],
  ['Chips va gazaklar', true, [
    ['Chips va suxariki', ['01211', '08422', '02004']],
    ["Pista va urug'lar", ['02008']],
  ]],
  ['Bolalar uchun', true, [
    ['Tagliklar', ['09619', '06211']],
    ['Bolalar ovqati', ['02104', '10406']],
    ["Butilka va so'rg'ichlar", ['03923', '04014', '07020']],
    ["O'yinchoqlar", ['09503', '09505', '04016']],
  ]],
  ["Gigiena va go'zallik", true, [
    ['Soch parvarishi', ['03305']],
    ['Dezodorant va atir', ['03307', '03303']],
    ['Tish parvarishi', ['03306', '09603']],
    ['Tana va yuz parvarishi', ['03304']],
    ["Salfetka va qog'oz", ['04803', '04818', '06307', '03005']],
  ]],
  ['Maishiy kimyo', true, [
    ['Sovun va kir yuvish', ['03401', '02836']],
    ['Idish va uy tozalash', ['03402', '03405', '03406']],
    ['Hasharotlarga qarshi', ['03808']],
    ['Tozalash anjomlari', ['04421']],
  ]],
  ["Uy-ro'zg'or", true, [
    ['Idish-tovoq', ['06911', '06912', '07013', '06203', '02815', '09617', '03924', '03926', '04419', '07323', '07326']],
    ['Elektr va batareyalar', ['08506', '08509', '08513', '08516', '08539']],
    ['Folga, paket va plyonka', ['04806', '07607', '03919']],
    ["Gugurt, zajigalka, ko'mir", ['03605', '09613', '02701', '03907', '04402', '07804']],
    ['Pichoq, qaychi, ustara', ['08211', '08212', '08213']],
    ['Sochiq va matolar', ['06302']],
  ]],
  ['Kanselyariya va kitoblar', true, [
    ["Daftar va qog'oz", ['04820', '08471']],
    ['Kitoblar', ['04903']],
    ['Kanselyariya', ['09608', '09609', '03213', '09017', '03214', '03506']],
  ]],
  ['Uy hayvonlari', true, [
    ['Hayvon ozuqasi', ['02309']],
  ]],
  ['Tamaki', false, [
    ['Sigaretlar', ['02402', '02403']],
  ]],
  ['Pivo', false, [
    ['Pivo', ['02203']],
  ]],
  ['Boshqa', true, [
    ['Boshqa', []],
  ]],
];

// Qo'lda qo'yilgan, ma'noli eski kategoriyalar (kichik harfda) → ichki bo'lim.
const OLD = {
  sigaret: 'Sigaretlar', suvlar: 'Suv', non: 'Non', tuxumlar: 'Tuxum', pampers: 'Tagliklar',
  'sut maxsulotla': 'Sut va kefir', 'kir sovun': 'Sovun va kir yuvish', prepravalar: "Ziravorlar va qo'shimchalar",
  piva: 'Pivo',
};
// Ichki/xizmat kategoriyalari: mahsulotlari joyida qoladi, kategoriya ilovadan yashiriladi.
const KEEP_HIDDEN = ['chiqmaydiganlar', 'summa', 'test', 'xizmatlar'];

// Nomdagi kalit so'z → ichki bo'lim (MXIK topilmasa).
const KEYWORDS = [
  [/sigaret|parlament|marlboro|winston|kent\b/i, 'Sigaretlar'],
  [/pivo|baltika|sarbast/i, 'Pivo'],
  [/muzqaymoq|morojen/i, 'Muzqaymoq'],
  [/\bnon\b|buxanka|lavash|baton/i, 'Non'],
  [/tuxum/i, 'Tuxum'],
  [/\bsut\b|kefir|qatiq/i, 'Sut va kefir'],
  [/yogurt|activia/i, 'Yogurt va desertlar'],
  [/pishloq|brinza|tvorog/i, 'Pishloq va tvorog'],
  [/kolbasa|sosiska|sardelka/i, 'Kolbasa va sosiskalar'],
  [/tovuq|go'sht|qiyma/i, "Go'sht va parranda"],
  [/shokolad|shkolad/i, 'Shokolad'],
  [/konfet/i, 'Konfetlar'],
  [/pechen|keks|rulet|vafli/i, 'Pechenye, keks va rulet'],
  [/choy|tea\b/i, 'Choy'],
  [/qahva|kofe|coffee|nescafe/i, 'Qahva'],
  [/\bsok\b|sharbat|juice/i, 'Sharbatlar'],
  [/\bsuv\b|water/i, 'Suv'],
  [/cola|fanta|sprite|pepsi|7up|energy/i, 'Gazli ichimliklar'],
  [/guruch|grechka|gerkules|yorma/i, 'Guruch va yormalar'],
  [/\bun\b/i, 'Un'],
  [/makaron|spagetti|rolton|lagmon/i, 'Makaron va tez tayyor'],
  [/yog'|moy/i, "O'simlik yog'i"],
  [/shakar|\btuz\b/i, 'Shakar va tuz'],
  [/ketchup|mayonez|sous/i, 'Souslar va ketchup'],
  [/chips|suxari|kirieshki|popkorn|pop korn/i, 'Chips va suxariki'],
  [/pista|semechka|urug/i, "Pista va urug'lar"],
  [/pampers|taglik|podguz/i, 'Tagliklar'],
  [/shampun|shampoo/i, 'Soch parvarishi'],
  [/tish|colgate|blend-a-med/i, 'Tish parvarishi'],
  [/salfetka|bumaga|qog'oz/i, "Salfetka va qog'oz"],
  [/sovun|ariel|tide|persil|kir yuv/i, 'Sovun va kir yuvish'],
  [/batareyka|lampochka|lampa/i, 'Elektr va batareyalar'],
  [/daftar|ruchka|qalam|kley/i, 'Kanselyariya'],
  [/olma|banan|apelsin|limon|uzum|nok\b/i, 'Mevalar'],
  [/kartoshka|piyoz|sabzi|savzi|pomidor|pamidor|bodring|karam/i, 'Sabzavotlar'],
  [/bl[ao]knot|papka|fayl|flamaster|kraska|puzl|albom/i, 'Kanselyariya'],
  [/plombir|eskimo|morojnoe/i, 'Muzqaymoq'],
  [/kasha|nutrila|nestogen|bellakt|fruto|nyanya|\bnan\b|semper|hipp|agusha/i, 'Bolalar ovqati'],
  [/soska|butilka/i, "Butilka va so'rg'ichlar"],
  [/shotka|zubachistka|parodont/i, 'Tish parvarishi'],
  [/rexona|deodorant|dezodorant|axe\b|atir/i, 'Dezodorant va atir'],
  [/garnier|nivea|dove|loreal|lorein|krem/i, 'Tana va yuz parvarishi'],
  [/uxochistka|vatniy|prokladka/i, "Salfetka va qog'oz"],
  [/domestos|domestis|fairy|aos\b|gupka|machalka/i, 'Idish va uy tozalash'],
  [/perchatka/i, 'Tozalash anjomlari'],
  [/mentos|chupa|chupy|tic tac|\btic\b|konfet|halvo|chak chak|harribo|haribo/i, 'Konfetlar'],
  [/milka|kinder|snickers|mars\b|twix|bounty|alpen/i, 'Shokolad'],
  [/\blays\b|pringles|cheers|crispy/i, 'Chips va suxariki'],
  [/felix|kitekat|whiskas|pedigree/i, 'Hayvon ozuqasi'],
  [/zajigalka|gugurt/i, "Gugurt, zajigalka, ko'mir"],
  [/strech|falga|folga|paket/i, 'Folga, paket va plyonka'],
  [/\besse\b|marlbo|camel|\bld\b/i, 'Sigaretlar'],
  [/moxito|mojito|chernogolovka|fanta|sprite|\bburn\b|flash|gorilla/i, 'Gazli ichimliklar'],
  [/chortoq|hydrolife|aquella|bonaqua/i, 'Suv'],
  [/smetana|sirok|tvorog|lactel|bulichka/i, 'Sut va kefir'],
  [/tushonka|kippers|shproti|sardina/i, "Go'sht va baliq konservalari"],
  [/jele|djem|jem\b|marmelad/i, 'Marmelad va jem'],
  [/lapsha|barilla|vermishel/i, 'Makaron va tez tayyor'],
  [/karkade|lipton|ahmad|greenfield/i, 'Choy'],
  [/heinz|karnishon|kornishon/i, 'Sabzavot konservalari'],
];

// Brend (nomning birinchi so'zi) va ishlab chiqaruvchi (shtrix-kodning 7 raqami) bo'yicha qo'shnilar:
// MXIK'siz mahsulot o'sha brend/ishlab chiqaruvchining boshqa mahsulotlari ko'p tushgan bo'limga tushadi.
const brandKey = (name) => (norm(name).match(/[a-z'ʻʼа-яё]{3,}/i) || [])[0];
const makerKey = (bc) => (/^\d{12,14}$/.test(String(bc || '').trim()) ? String(bc).trim().slice(0, 7) : null);
// 18+ (yashirin) bo'limlar kalit so'zi MXIK va qo'shnilardan ustun — sigaret/pivo ochiq bo'limga tushib qolmasin.
const adultLeaf = (name) => KEYWORDS.find(([re, l]) => (l === 'Sigaretlar' || l === 'Pivo') && re.test(name))?.[1];

function majority(votes) {
  if (!votes) return null;
  const entries = Object.entries(votes).sort((a, b) => b[1] - a[1]);
  const total = entries.reduce((s, [, n]) => s + n, 0);
  const [leaf, n] = entries[0];
  return n >= 2 && n / total >= 0.6 ? leaf : null; // kamida 2 ta va 60% ustunlik
}

const prefixToLeaf = new Map();
for (const [, , children] of TREE)
  for (const [leaf, prefixes] of children)
    for (const p of prefixes) {
      if (prefixToLeaf.has(p)) throw new Error(`MXIK prefiksi ikki marta: ${p}`);
      prefixToLeaf.set(p, leaf);
    }

const norm = (s) => String(s ?? '').trim().toLowerCase();

async function api(method, path, token, body) {
  const res = await fetch(apiBase + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${text.slice(0, 300)}`);
  return text ? JSON.parse(text) : null;
}

const { token } = await api('POST', '/api/auth/login', null, {
  username: process.env.POS_USER || 'admin', password: process.env.POS_PASS || 'Admin12345!',
});
let categories = await api('GET', '/api/categories', token);
const products = await api('GET', '/api/products', token);
const catById = new Map(categories.map((c) => [c.id, c]));
console.log(`mahsulot: ${products.length}, kategoriya: ${categories.length}, rejim: ${APPLY ? 'APPLY' : 'faqat hisobot'}`);

// 1) Har bir mahsulot uchun ichki bo'limni tanlash.
const plan = []; // { p, leaf, how }
for (const p of products) {
  const oldName = norm(catById.get(p.categoryId)?.name);
  if (catById.get(p.categoryId)?.parentId) continue; // allaqachon daraxtda — tegmaymiz
  if (KEEP_HIDDEN.includes(oldName)) continue;
  let leaf = OLD[oldName], how = 'eski kategoriya';
  if (!leaf) { leaf = adultLeaf(p.name); how = "kalit so'z (18+)"; }
  if (!leaf) { leaf = prefixToLeaf.get(String(p.mxikCode || '').slice(0, 5)); how = 'MXIK'; }
  plan.push({ p, leaf, how });
}

// 2-o'tish: aniq usullar bilan topilganlardan brend/ishlab chiqaruvchi "ovozlari" — keyin qolganlarga.
const brandVotes = {}, makerVotes = {};
const vote = (store, key, leaf) => { if (key) ((store[key] ||= {})[leaf] = (store[key][leaf] || 0) + 1); };
for (const x of plan) if (x.leaf) { vote(brandVotes, brandKey(x.p.name), x.leaf); vote(makerVotes, makerKey(x.p.barcode), x.leaf); }
for (const x of plan) {
  if (x.leaf) continue;
  const p = x.p;
  let leaf = majority(brandVotes[brandKey(p.name)]), how = 'brend';
  if (!leaf) { leaf = majority(makerVotes[makerKey(p.barcode)]); how = 'ishlab chiqaruvchi'; }
  if (!leaf) { leaf = KEYWORDS.find(([re]) => re.test(p.name))?.[1]; how = "kalit so'z"; }
  if (!leaf) { leaf = 'Boshqa'; how = 'topilmadi'; }
  x.leaf = leaf; x.how = how;
}

// Hisobot
const byLeaf = {}, byHow = {};
for (const x of plan) { byLeaf[x.leaf] = (byLeaf[x.leaf] || 0) + 1; byHow[x.how] = (byHow[x.how] || 0) + 1; }
console.log('usul:', byHow);
for (const [parent, online, children] of TREE) {
  const total = children.reduce((s, [leaf]) => s + (byLeaf[leaf] || 0), 0);
  console.log(`${online ? ' ' : '🚫'} ${parent} — ${total}: ` + children.map(([leaf]) => `${leaf} ${byLeaf[leaf] || 0}`).join(', '));
}
const csv = ['Mahsulot;Shtrix-kod;MXIK;Eski kategoriya;Yangi bo\'lim;Usul',
  ...plan.map(({ p, leaf, how }) => [p.name, p.barcode || '', p.mxikCode || '', catById.get(p.categoryId)?.name || '', leaf, how]
    .map((v) => `"${String(v).replace(/"/g, '""')}"`).join(';'))];
fs.writeFileSync('categorize-report.csv', '﻿' + csv.join('\n'));
console.log('hisobot: categorize-report.csv');
if (!APPLY) process.exit(0);

// 2) Bo'limlarni yaratish/yangilash (nom + ota bo'yicha topiladi, bo'lmasa yaratiladi).
const find = (name, parentId) => categories.find((c) => norm(c.name) === norm(name) && (c.parentId ?? null) === (parentId ?? null));
async function upsert(name, parentId, sortOrder, showOnline) {
  const ex = find(name, parentId);
  const body = { name, parentId, sortOrder, showOnline, note: ex?.note ?? null, imageUrl: ex?.imageUrl ?? null };
  const c = ex ? await api('PUT', `/api/categories/${ex.id}`, token, body) : await api('POST', '/api/categories', token, body);
  if (!ex) categories.push(c); else Object.assign(ex, c);
  return c;
}
const leafId = new Map();
for (const [i, [parent, online, children]] of TREE.entries()) {
  const pc = await upsert(parent, null, (i + 1) * 10, online);
  for (const [j, [leaf]] of children.entries()) {
    const lc = await upsert(leaf, pc.id, j + 1, true);
    leafId.set(leaf, lc.id);
  }
}

// 3) Mahsulotlarni ko'chirish (1000 tadan).
let moved = 0;
const groups = {};
for (const { p, leaf } of plan) (groups[leafId.get(leaf)] ||= []).push(p.id);
for (const [categoryId, ids] of Object.entries(groups))
  for (let i = 0; i < ids.length; i += 1000)
    moved += (await api('PUT', '/api/products/bulk-category', token, { productIds: ids.slice(i, i + 1000), categoryId })).updated;
console.log(`ko'chirildi: ${moved}`);

// 4) Daraxtdan tashqaridagi eski asosiy kategoriyalarni ilovadan yashirish (o'chirmaymiz — kassada kerak bo'lishi mumkin).
const treeIds = new Set(categories.filter((c) => c.parentId || TREE.some(([n]) => norm(n) === norm(c.name))).map((c) => c.id));
for (const c of categories.filter((c) => !treeIds.has(c.id) && c.showOnline !== false)) {
  await upsert(c.name, null, c.sortOrder ?? 0, false);
  console.log(`yashirildi: ${c.name}`);
}
console.log('tayyor');
