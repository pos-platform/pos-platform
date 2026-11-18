// Serverdan kelgan loyalty sozlamalarini PUT /api/loyalty/settings uchun to'liq payloadga aylantiradi.
// Ro'yxat sahifasidagi Faol/Passiv tugmasi va tahrirlash sahifasi bitta formatdan foydalanadi.
export function settingsToPayload(s, overrides = {}) {
  const expire = s.pointsExpireDays
  return {
    enabled: !!s.enabled,
    cashbackPercent: Number(s.cashbackPercent ?? 0),
    minSaleAmount: Number(s.minSaleAmount ?? 0),
    maxRedeemPercent: Number(s.maxRedeemPercent ?? 0),
    pointsExpireDays: expire === '' || expire === null || expire === undefined ? null : Number(expire),
    signupBonus: Number(s.signupBonus ?? 0),
    earnOnDiscountedItems: s.earnOnDiscountedItems !== false,
    categoryRates: (s.categoryRates || []).map((r) => ({ categoryId: r.categoryId, percent: Number(r.percent) })),
    tiers: [...(s.tiers || [])]
      .sort((a, b) => Number(a.minSpend) - Number(b.minSpend))
      .map((t) => ({
        ...(t.id ? { id: t.id } : {}),
        name: (t.name || '').trim(),
        minSpend: Number(t.minSpend),
        cashbackPercent: Number(t.cashbackPercent)
      })),
    ...overrides
  }
}
