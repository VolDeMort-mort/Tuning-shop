// AC-20 (NFR-01): 100 запитів списку поточних замовлень і пошуку на базі з 5 000 замовлень; p95 ≤ 2 с.
// Підготовка: dotnet TuningShop.Api.dll seed-volume 5000, потім запуск сервера на BASE_URL.

import { writeFileSync, mkdirSync } from "node:fs";

const BASE = process.env.BASE_URL ?? "http://localhost:5081";
const N = 100;

const login = await fetch(`${BASE}/api/auth/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ login: "manager", password: "manager123" }),
});
const cookie = login.headers.get("set-cookie").split(";")[0];

async function measure(name, url) {
  const times = [];
  let size = 0;
  for (let i = 0; i < N; i++) {
    const started = performance.now();
    const response = await fetch(`${BASE}${typeof url === "function" ? url(i) : url}`, { headers: { cookie } });
    const body = await response.json();
    times.push(performance.now() - started);
    if (!response.ok) throw new Error(`${name}: HTTP ${response.status}`);
    size = Array.isArray(body) ? body.length : 0;
  }
  times.sort((a, b) => a - b);
  const p95 = times[Math.ceil(0.95 * N) - 1];
  const result = { name, requests: N, rows: size, p50: times[N / 2 - 1], p95, max: times[N - 1], ok: p95 <= 2000 };
  console.log(`${name}: rows=${size}, p50=${result.p50.toFixed(1)} мс, p95=${p95.toFixed(1)} мс, max=${result.max.toFixed(1)} мс → ${result.ok ? "≤ 2 с" : "ПЕРЕВИЩЕНО"}`);
  return result;
}

const results = [
  await measure("Список поточних замовлень", "/api/orders/current"),
  await measure("Пошук клієнта за частиною ПІБ", (i) => `/api/clients?query=${encodeURIComponent("Тестовий 0" + String(i).padStart(3, "0"))}`),
  await measure("Пошук клієнта за телефоном", (i) => `/api/clients?query=${encodeURIComponent("+38099000" + String(1000 + i * 7).padStart(4, "0"))}`),
  await measure("Пошук автомобіля за держномером", (i) => `/api/cars?query=PF${String(100 + i * 13).padStart(6, "0")}`),
];

mkdirSync("results", { recursive: true });
writeFileSync("results/perf-nfr01.json", JSON.stringify({ date: new Date().toISOString(), base: BASE, results }, null, 2));
process.exitCode = results.every((r) => r.ok) ? 0 : 1;
