# Запуск пакета «Тюнінг-ательє» v1.0.0

Пакет містить зібраний сервер (ASP.NET Core 8) і клієнт (статичний веб-інтерфейс у `wwwroot/`).
Сервер сам застосовує міграції SQLite та створює облікові записи працівників під час першого запуску.

## Вимоги

- .NET 8 Runtime (ASP.NET Core) — `dotnet --list-runtimes` має показувати `Microsoft.AspNetCore.App 8.x`.
- Сучасний браузер (Chrome, Firefox або Edge), ширина екрана від 1280 px.

## Запуск

Розпакуйте архів у новий каталог і виконайте команди в ньому. Шлях до бази краще задавати абсолютним:
відносний шлях розв'язується від каталогу пакета.

Windows (PowerShell):

```powershell
$env:ConnectionStrings__Tuning = "Data Source=C:\tuning-data\tuning-shop.db"
dotnet TuningShop.Api.dll seed-demo        # лише для демонстраційної бази: тестові клієнти й замовлення
dotnet TuningShop.Api.dll --urls http://localhost:5080
```

Linux / macOS:

```bash
export ConnectionStrings__Tuning="Data Source=/opt/tuning-data/tuning-shop.db"
dotnet TuningShop.Api.dll seed-demo
dotnet TuningShop.Api.dll --urls http://localhost:5080
```

Відкрийте http://localhost:5080. Перевірка доступності сервера: http://localhost:5080/health → `{"status":"ok"}`.

## Облікові записи після розгортання

| Роль | Логін | Пароль |
| --- | --- | --- |
| Менеджер | `manager` | `manager123` |
| Майстер | `master1`, `master2` | `master123` |
| Керівник | `director` | `director123` |

Клієнт відстежує замовлення без входу на сторінці «Відстежити замовлення» (демо: `TS-DEMO-0002`, `+380671234567`).

## Службові команди

| Команда | Призначення |
| --- | --- |
| `dotnet TuningShop.Api.dll seed-demo` | Додати демонстраційні дані до порожньої бази |
| `dotnet TuningShop.Api.dll seed-volume 5000` | Згенерувати 5 000 замовлень для перевірки NFR-01 |
| `dotnet TuningShop.Api.dll backup <файл>` | Резервна копія бази (NFR-06); відновлення — заміна файла бази копією при зупиненому сервері |

Оновлення пакета не змінює файл бази: замініть файли застосунку, залишивши той самий `ConnectionStrings__Tuning`.
