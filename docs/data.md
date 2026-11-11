# Дані, схема й конфігурація v1.0

## Сховище

SQLite 3 (файл), доступ через Entity Framework Core 8. Схему відтворює міграція [`InitialV1`](../backend/src/TuningShop.Api/Data/Migrations), яку сервер застосовує під час запуску (`Database.Migrate()`). Під час першого запуску створюються облікові записи працівників ([`DbInitializer`](../backend/src/TuningShop.Api/Data/DbInitializer.cs)).

| Таблиця | Клас | Ключові обмеження |
| --- | --- | --- |
| `Clients` | `Client` | `Phone` — унікальний індекс; індекс `FullNameSearch` |
| `Cars` | `Car` | `Vin` — унікальний індекс; індекс `PlateNumber`; FK `ClientId` (обов'язковий, Restrict) |
| `Orders` | `Order` | `Number` — унікальний індекс; індекс `Status`; FK `CarId` (обов'язковий, Restrict) |
| `WorkItems` | `WorkItem` | FK `OrderId` (обов'язковий, Cascade — композиція); FK `MasterId` → `Employees` (необов'язковий, Restrict) |
| `Parts` | `Part` | FK `WorkItemId` (обов'язковий, Cascade — композиція) |
| `StatusChanges` | `StatusChange` | FK `OrderId` (обов'язковий, Cascade); FK `EmployeeId` (обов'язковий, Restrict) |
| `Employees` | `Employee` | `Login` — унікальний індекс; зберігається лише `PasswordHash` |

Подання значень: переліки — рядки (`New`, `InProgress`, …); моменти часу — UTC (після читання `DateTimeKind.Utc`); `decimal` — текст SQLite без втрати точності; `DateOnly` — `yyyy-MM-dd`.

Правило «активне замовлення одне на автомобіль» (FR-06) і переходи станів виконує предметний код до запису; обмеження бази доповнюють, а не замінюють ці перевірки. Конкурентні записи кількох менеджерів в одне замовлення в v1.0 не розглядаються.

## Зміна схеми

```bash
cd backend
dotnet tool restore                      # dotnet-ef 8.0.11 з .config/dotnet-tools.json
dotnet dotnet-ef migrations add <Назва> --project src/TuningShop.Api --output-dir Data/Migrations
```

Створення файла міграції не змінює жодної бази. Схема оновлюється під час наступного запуску сервера з цією базою. Збереження нових записів міграцій не потребує.

## Конфігурація

| Параметр | Значення за замовчуванням | Як змінити |
| --- | --- | --- |
| `ConnectionStrings:Tuning` | `Data Source=data/tuning-shop.db` (відносно каталогу застосунку) | змінна середовища `ConnectionStrings__Tuning`, бажано з абсолютним шляхом |
| Адреса сервера | `http://localhost:5080` (профіль розробки) | `--urls http://host:port` |
| `ASPNETCORE_ENVIRONMENT` | `Production` у пакеті, `Development` у `dotnet run` | змінна середовища; Swagger доступний лише в `Development` |
| `BACKEND_URL` (клієнт, `next dev`) | `http://localhost:5080` | змінна середовища під час розробки клієнта |

## Службові команди

| Команда | Призначення |
| --- | --- |
| `seed-demo` | Демонстраційна база: 3 клієнти, 4 автомобілі, замовлення в усіх статусах (`TS-DEMO-0001…0005`) |
| `seed-volume <N>` | `N` згенерованих замовлень для перевірки NFR-01 |
| `backup <файл>` | Онлайн-копія бази (SQLite Online Backup API) для NFR-06 |

## Облікові записи після розгортання

`manager` / `manager123` (Менеджер), `master1`, `master2` / `master123` (Майстер), `director` / `director123` (Керівник). Це навчальні облікові записи: для реального розгортання паролі слід змінити.
