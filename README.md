# Tuning-shop — система управління замовленнями тюнінг-ательє

Навчальний проєкт з дисципліни «Технології розроблення програмного забезпечення», група ТР-41.
Сервер — ASP.NET Core 8 + Entity Framework Core + SQLite; клієнт — Next.js (статичний веб-інтерфейс).

## Документація

| Розділ | Файл |
| --- | --- |
| Вимоги (SRS), базова лінія | [docs/docs/requirements.md](docs/docs/requirements.md) — теги `requirements-baseline-v1.0`, `requirements-baseline-v1.1` (CR-01) |
| Варіанти використання, проєктування | [docs/docs/use-cases.md](docs/docs/use-cases.md), [docs/docs/design.md](docs/docs/design.md), [docs/model/](docs/model/) |
| Матриця простежуваності (матриця ЛР №3, доповнена в ЛР №4) | [docs/docs/traceability.md](docs/docs/traceability.md) |
| План реалізації | [docs/docs/implementation-plan.md](docs/docs/implementation-plan.md) |
| Програмний інтерфейс | [docs/api.md](docs/api.md) |
| Дані, міграції, конфігурація | [docs/data.md](docs/data.md) |
| Фактичні моделі | [docs/models/implementation.md](docs/models/implementation.md), [docs/models/sequences/](docs/models/sequences/) |
| Випуск v1.0.0 і результати перевірок | [docs/releases/v1.0.0.md](docs/releases/v1.0.0.md) |
| Журнал процесу | [docs/process/process-log.md](docs/process/process-log.md) |
| Покажчик ЛР №4 | [docs/reports/lab-04.md](docs/reports/lab-04.md) |

## Структура

```
backend/
  src/TuningShop.Api/      сервер: Domain, Application, Data (міграції), Contracts, Controllers
  tests/TuningShop.Tests/  модульні, інтеграційні (SQLite) і HTTP-перевірки
frontend/                  клієнт Next.js (app/, components/, lib/)
e2e/                       приймальний прогін через інтерфейс і замір NFR-01
scripts/release.ps1        збирання пакета випуску
docs/                      документація
```

## Розробка

Потрібно: .NET SDK 8+ (збирання net8.0), Node.js 20+.

```bash
# сервер на http://localhost:5080 (база — backend/src/TuningShop.Api/data/tuning-shop.db)
cd backend
dotnet run --project src/TuningShop.Api -- seed-demo   # один раз: демонстраційні дані
dotnet run --project src/TuningShop.Api

# клієнт на http://localhost:3000 (запити /api проксуються на :5080)
cd frontend
npm ci
npm run dev
```

Облікові записи: `manager` / `manager123`, `master1` / `master123`, `director` / `director123`. Відстеження без входу — сторінка «Відстежити замовлення» (демо: `TS-DEMO-0002`, `+380671234567`).

## Перевірки

```bash
cd backend && dotnet test                  # 99 автоматизованих перевірок
cd frontend && npm run lint && npm run build
cd e2e && npm install && node acceptance.mjs   # потребує запущеного пакета з seed-demo на :5080
```

CI (GitHub Actions) виконує збирання й тести сервера та збирання клієнта для запитів на злиття в `dev` і `main`.

## Випуск

`powershell -ExecutionPolicy Bypass -File scripts/release.ps1 -Version 1.0.0` збирає з чистого коміту сервер разом із клієнтом, архів і SHA-256. Запуск пакета описано в [docs/releases/RUN.md](docs/releases/RUN.md).
