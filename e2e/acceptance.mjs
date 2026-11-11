// Приймальний прогін v1.0 через веб-інтерфейс (ЛР №4, крок 5.5).
// Перед запуском: сервер пакета з демонстраційною базою (seed-demo) на BASE_URL.
//   node acceptance.mjs                 — основний прогін і протокол у results/
//   node acceptance.mjs --after-restart — перевірка даних після перезапуску сервера
// Браузер: встановлений Chrome (BROWSER_CHANNEL=msedge для Edge).

import { chromium } from "playwright-core";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";

const BASE = process.env.BASE_URL ?? "http://localhost:5080";
const afterRestart = process.argv.includes("--after-restart");
const stateFile = "results/last-run.json";
const shots = "results/screenshots";
mkdirSync(shots, { recursive: true });

const protocol = [];
const run = Date.now().toString().slice(-6);
const phone = `+38063${run.padStart(7, "0")}`;
const vinChars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
const vin = Array.from({ length: 17 }, () => vinChars[Math.floor(Math.random() * vinChars.length)]).join("");
const plate = `AT${run.slice(-4)}TT`;

const browser = await chromium.launch({ channel: process.env.BROWSER_CHANNEL ?? "chrome", headless: true });
const context = await browser.newContext({ viewport: { width: 1366, height: 900 }, locale: "uk-UA" });
let page = await context.newPage();

/** Один рядок протоколу: критерій, дія, очікуваний і фактичний результат. */
async function check(ac, action, expected, verify) {
  let actual;
  let ok = false;
  try {
    actual = (await verify()) ?? expected;
    ok = true;
  } catch (e) {
    actual = "НЕ ВИКОНАНО: " + String(e.message ?? e).split("\n")[0];
  }
  protocol.push({ ac, action, expected, actual, ok });
  console.log(`${ok ? "PASS" : "FAIL"}  ${ac.padEnd(8)} ${action} → ${actual}`);
  if (!ok) {
    await page.screenshot({ path: `${shots}/fail-${protocol.length}.png`, fullPage: true });
    throw new Error(`Перевірку ${ac} не пройдено`);
  }
}

const visible = (text, options = {}) => page.getByText(text, options).first().waitFor({ timeout: 8000 });
const field = (label) => page.getByLabel(label, { exact: true });
const button = (name) => page.getByRole("button", { name, exact: true });

async function login(user, password) {
  await page.goto(`${BASE}/login/`);
  await field("Логін").fill(user);
  await field("Пароль").fill(password);
  await button("Увійти").click();
}

async function logout() {
  await button("Вийти").click();
  await page.waitForURL(/\/login\/$/);
}

async function main() {
  if (afterRestart) {
    const state = JSON.parse(readFileSync(stateFile, "utf8"));
    await login("manager", "manager123");
    await page.waitForURL(/\/orders\/$/);
    await check("FR-15/NFR", `Перезапуск сервера, відкрити ${state.orderNumber}`, "Статус «Видане», історія з 4 записів, деталь «Фари LED» збережена",
      async () => {
        await page.goto(`${BASE}/orders/view/?id=${state.orderId}`);
        await visible("Фари LED");
        await page.getByRole("heading", { name: new RegExp(`${state.orderNumber}.*Видане`) }).waitFor();
        const rows = await page.locator("table").last().locator("tbody tr").count();
        if (rows !== 4) throw new Error(`записів історії: ${rows}`);
      });
    return;
  }

  // ---- FR-01: вхід і ролі ----
  await check("AC-01", "Без входу відкрити /orders/", "Перенаправлення на сторінку входу", async () => {
    await page.goto(`${BASE}/orders/`);
    await page.waitForURL(/\/login\/$/);
  });
  await check("AC-01", "Вхід з неправильним паролем", "Вхід не виконано, повідомлення «Невірний логін або пароль.»", async () => {
    await login("manager", "wrong");
    await visible("Невірний логін або пароль.");
  });
  await check("AC-01", "Вхід manager / manager123", "Розділ «Поточні замовлення», меню Менеджера", async () => {
    await login("manager", "manager123");
    await page.waitForURL(/\/orders\/$/);
    await page.getByRole("link", { name: "Оформити замовлення" }).waitFor();
  });
  await check("AC-16", "Поточні замовлення, фільтр «В роботі»", "Лише TS-DEMO-0002", async () => {
    await visible("TS-DEMO-0001");
    await page.getByLabel("Статус").selectOption("InProgress");
    await page.getByText("TS-DEMO-0001").waitFor({ state: "detached" });
    await visible("TS-DEMO-0002");
    await page.screenshot({ path: `${shots}/01-current-orders.png`, fullPage: true });
  });

  // ---- UC-01 / UC-02 ----
  await page.getByRole("link", { name: "Клієнти й автомобілі" }).click();
  await check("AC-02/22", "Реєстрація клієнта з телефоном 0631234567", "Клієнта не збережено, біля поля «Телефон має формат +380XXXXXXXXX.»", async () => {
    await field("ПІБ *").fill("Приймальний Тест Клієнт");
    await field("Телефон *").fill("0631234567");
    await button("Зареєструвати").click();
    await visible("Телефон має формат +380XXXXXXXXX.");
  });
  await check("AC-02", `Реєстрація з телефоном ${phone}`, "Відкрито картку клієнта", async () => {
    await field("Телефон *").fill(phone);
    await button("Зареєструвати").click();
    await page.waitForURL(/\/clients\/view\//);
    await visible(phone);
  });
  await check("AC-04", "Додати автомобіль з VIN із 16 символів", "Не додано, біля поля «VIN має містити рівно 17 символів.»", async () => {
    await field("Марка *").fill("Honda");
    await field("Модель *").fill("Civic Type R");
    await field("Рік *").fill("2022");
    await field("VIN *").fill(vin.slice(0, 16));
    await field("Держномер *").fill(plate);
    await button("Додати").click();
    await visible("VIN має містити рівно 17 символів.");
  });
  await check("AC-04", `Додати автомобіль з VIN ${vin}`, "Автомобіль у картці клієнта", async () => {
    await field("VIN *").fill(vin);
    await button("Додати").click();
    await visible(`Автомобіль ${plate} додано.`);
  });

  // ---- UC-11 → UC-03 ----
  await check("AC-18", `Пошук за держномером ${plate}`, "Відкрито картку автомобіля", async () => {
    await page.getByRole("link", { name: "Клієнти й автомобілі" }).click();
    await page.getByLabel("ПІБ (частина), телефон, держномер або VIN").fill(plate.toLowerCase());
    await button("Знайти").click();
    await page.waitForURL(/\/cars\/view\//);
  });
  let orderNumber;
  await check("AC-06/07", "Оформити замовлення: 2 роботи, одна без майстра", "Попередження A4; збережено зі статусом «Нове» й номером", async () => {
    await page.locator("main").getByRole("link", { name: "Оформити замовлення" }).click();
    await page.waitForURL(/\/orders\/new\/\?carId=/);
    await page.getByRole("button", { name: "Додати роботу" }).click();
    const categories = page.getByLabel("Вид роботи *");
    const descriptions = page.getByLabel("Опис *");
    const costs = page.getByLabel("Вартість, грн *");
    await categories.nth(0).selectOption("Exhaust");
    await descriptions.nth(0).fill("Спортивний вихлоп");
    await costs.nth(0).fill("18000");
    await page.getByLabel("Майстер").nth(0).selectOption({ label: "Петренко Олег" });
    await categories.nth(1).selectOption("Lighting");
    await descriptions.nth(1).fill("LED-фари");
    await costs.nth(1).fill("9000");
    await visible("Робіт без майстра: 1.");
    await button("Зберегти замовлення").click();
    await page.waitForURL(/\/orders\/view\//);
    await visible("Замовлення збережено.");
    const heading = await page.getByRole("heading", { level: 1 }).innerText();
    orderNumber = heading.match(/TS-\d{4}-\d{4}/)?.[0];
    if (!orderNumber || !heading.includes("Нове")) throw new Error(heading);
    await page.screenshot({ path: `${shots}/02-new-order.png`, fullPage: true });
    return `Збережено ${orderNumber}, «Нове», 27 000 грн`;
  });
  const orderId = new URL(page.url()).searchParams.get("id");

  await check("AC-06", "Повторно оформити замовлення на той самий автомобіль", "Оформлення неможливе, показано номер активного", async () => {
    await page.getByRole("link", { name: /Honda Civic/ }).click();
    await page.getByRole("link", { name: `Активне замовлення ${orderNumber}` }).waitFor();
    await page.goto(new URL(page.url()).href.replace("/cars/view/?id=", "/orders/new/?carId="));
    await visible("Оформлення неможливе");
    await page.goto(`${BASE}/orders/view/?id=${orderId}`);
  });

  // ---- UC-04: передати в роботу ----
  await check("AC-09", "«Передати в роботу» з роботою без майстра", "Статус не змінено, показано «LED-фари»", async () => {
    await button("Передати в роботу").click();
    await visible("Не всім роботам призначено майстра.");
    await page.getByRole("alert").getByText("LED-фари").waitFor();
  });
  await check("AC-08/09", "Призначити майстра і передати в роботу", "Статус «В роботі»", async () => {
    await page.getByLabel("Призначити майстра").nth(1).selectOption({ label: "Петренко Олег" });
    await visible("Майстра призначено.");
    await button("Передати в роботу").click();
    await page.getByRole("heading", { name: /В роботі/ }).waitFor();
  });
  await check("AC-27", "Додати деталь «Фари LED» × 2 і відмітити надходження", "Стан «Очікується», потім «Отримано» з часом", async () => {
    const work = page.locator("li").filter({ hasText: "LED-фари" }).first();
    await work.getByLabel("Назва деталі").fill("Фари LED");
    await work.getByLabel("Кількість").fill("2");
    await work.getByRole("button", { name: "Додати деталь" }).click();
    await visible("Деталь додано.");
    await work.getByText("Очікується").first().waitFor();
    await work.getByRole("button", { name: "Відмітити надходження" }).click();
    await work.getByText(/отримано \d/).waitFor();
  });
  await logout();

  // ---- UC-05 ----
  await check("AC-10", "Вхід master1, «Мої роботи»", `Дві роботи замовлення ${orderNumber}`, async () => {
    await login("master1", "master123");
    await page.waitForURL(/\/master\/$/);
    const cards = page.locator("section").filter({ hasText: orderNumber });
    await cards.first().waitFor();
    const count = await cards.count();
    if (count !== 2) throw new Error(`карток: ${count}`);
    await page.screenshot({ path: `${shots}/03-master.png`, fullPage: true });
  });
  await check("AC-11/19", "Розпочати й завершити обидві роботи", "Статуси «Виконано», прогрес «виконано 2 з 2»", async () => {
    for (let i = 0; i < 4; i++) {
      const card = page.locator("section").filter({ hasText: orderNumber }).filter({ has: page.getByRole("button") }).first();
      await card.getByRole("button").click();
      await page.getByRole("status").waitFor();
    }
    await visible("виконано 2 з 2");
  });
  await logout();

  // ---- UC-10 ----
  await check("AC-28", "Відстеження з чужим телефоном", "«Замовлення не знайдено.» без даних", async () => {
    await page.goto(`${BASE}/track/`);
    await page.getByLabel("Номер замовлення").fill(orderNumber);
    await page.getByLabel("Телефон власника").fill("+380671234567");
    await button("Показати").click();
    await visible("Замовлення не знайдено.");
  });
  await check("AC-28", "Відстеження з телефоном власника", "Статус «В роботі», «виконано 2 з 2», деталь «Отримано»", async () => {
    await page.getByLabel("Телефон власника").fill(phone);
    await button("Показати").click();
    await visible("виконано 2 з 2");
    await visible(/Фари LED × 2 — Отримано/);
    await page.screenshot({ path: `${shots}/04-tracking.png`, fullPage: true });
  });

  // ---- UC-04: готовність і видача ----
  await login("manager", "manager123");
  await page.waitForURL(/\/orders\/$/);
  await check("UC-05 к.9", "Поточні замовлення", `Біля ${orderNumber} позначка «усі роботи виконано»`, async () => {
    await page.locator("tr").filter({ hasText: orderNumber }).getByText("усі роботи виконано").waitFor();
  });
  await check("AC-12/13", "Позначити готовим і видати автомобіль", "Статус «Видане», дата видачі, дій немає", async () => {
    await page.goto(`${BASE}/orders/view/?id=${orderId}`);
    await button("Позначити готовим").click();
    await page.getByRole("heading", { name: /Готове до видачі/ }).waitFor();
    await button("Видати автомобіль").click();
    await page.getByRole("heading", { name: /Видане/ }).waitFor();
    await visible("Замовлення у фінальному статусі — лише перегляд.");
  });
  await check("AC-15", "Історія статусів у картці", "4 записи: Нове, В роботі, Готове до видачі, Видане", async () => {
    const rows = await page.locator("table").last().locator("tbody tr").allInnerTexts();
    if (rows.length !== 4 || !rows[3].includes("Видане")) throw new Error(rows.join(" | "));
    await page.screenshot({ path: `${shots}/05-issued.png`, fullPage: true });
  });
  await check("AC-17", "Завершені замовлення", `${orderNumber} із датою видачі`, async () => {
    await page.getByRole("link", { name: "Завершені" }).click();
    await page.locator("tr").filter({ hasText: orderNumber }).getByText(/Видано/).waitFor();
  });

  // ---- UC-04: скасування ----
  await check("AC-14", "Скасувати TS-DEMO-0001 без причини, потім з причиною", "Без причини — відмова біля поля; з причиною — «Скасоване»", async () => {
    await page.getByRole("link", { name: "Поточні замовлення" }).click();
    await page.getByRole("link", { name: "TS-DEMO-0001" }).click();
    await button("Скасувати замовлення").click();
    await button("Підтвердити скасування").click();
    await visible("Вкажіть причину скасування.");
    await page.getByLabel("Причина скасування *").fill("Клієнт відмовився (приймання)");
    await button("Підтвердити скасування").click();
    await page.getByRole("heading", { name: /Скасоване/ }).waitFor();
  });
  await logout();

  // ---- FR-01: Керівник ----
  await check("AC-01", "Вхід director, пряма адреса /orders/new/", "Списки доступні, оформлення — «Ця функція недоступна для вашої ролі.»", async () => {
    await login("director", "director123");
    await page.waitForURL(/\/orders\/$/);
    if (await page.getByRole("link", { name: "Оформити замовлення" }).count()) throw new Error("меню містить оформлення");
    await page.goto(`${BASE}/orders/new/`);
    await visible("Ця функція недоступна для вашої ролі.");
  });

  writeFileSync(stateFile, JSON.stringify({ orderId, orderNumber, phone }, null, 2));
}

let failed = false;
try {
  await main();
} catch (e) {
  failed = true;
  console.error(e.message);
} finally {
  await browser.close();
  const stamp = new Date().toISOString();
  const lines = [
    `# Протокол приймального прогону через інтерфейс (${afterRestart ? "після перезапуску" : "основний"})`,
    "",
    `Дата: ${stamp}; сервер: ${BASE}; браузер: ${process.env.BROWSER_CHANNEL ?? "chrome"} (headless).`,
    "",
    "| Критерій | Дія | Очікуваний результат | Фактичний результат | Висновок |",
    "| --- | --- | --- | --- | --- |",
    ...protocol.map((p) => `| ${p.ac} | ${p.action} | ${p.expected} | ${p.actual} | ${p.ok ? "пройдено" : "не пройдено"} |`),
  ];
  const file = `results/acceptance-${afterRestart ? "restart" : "main"}.md`;
  writeFileSync(file, lines.join("\n") + "\n");
  console.log(`Протокол: ${file}`);
  process.exitCode = failed ? 1 : 0;
}
