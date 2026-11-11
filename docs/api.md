# Програмний інтерфейс v1.0

Сервер: ASP.NET Core 8, базова адреса `http://localhost:5080`. Формат — JSON (UTF-8); переліки передаються рядками (`"InProgress"`), моменти часу — ISO 8601 в UTC із кінцевим `Z`, дати — `yyyy-MM-dd`. Опис для перегляду в середовищі розробки — Swagger: `/swagger`.

Автентифікація — cookie `tuning_session` після `POST /api/auth/login`; тайм-аут бездіяльності 30 хв (NFR-02). Роль перевіряється на сервері для кожного запиту (FR-01).

## Маршрути

| Метод і маршрут | Ролі | Призначення | Успіх | Передбачені відмови |
| --- | --- | --- | --- | --- |
| `POST /api/auth/login` `{login, password}` | усі | Вхід (FR-01) | 200 `CurrentUserDto` | 400 `invalid_credentials` |
| `POST /api/auth/logout` | усі | Вихід | 204 | — |
| `GET /api/auth/me` | працівник | Поточний користувач | 200 `CurrentUserDto` | 401 |
| `GET /api/clients?query=` | Менеджер, Керівник | Пошук: повний телефон або частина ПІБ (FR-18) | 200 `ClientDto[]` | — |
| `GET /api/clients/{id}` | Менеджер, Керівник | Картка клієнта з автомобілями й замовленнями | 200 `ClientDetailsDto` | 404 `client_not_found` |
| `POST /api/clients` `{fullName, phone, email?}` | Менеджер | Реєстрація (FR-02) | 201 `ClientDto` | 400 `validation_failed`; 409 `phone_exists` |
| `PUT /api/clients/{id}` | Менеджер | Редагування (FR-03) | 200 `ClientDto` | 400, 404, 409 `phone_exists` |
| `POST /api/clients/{id}/cars` `{make, model, year, vin, plateNumber}` | Менеджер | Додати автомобіль (FR-04) | 201 `CarDto` | 400; 404 `client_not_found`; 409 `vin_exists` |
| `GET /api/cars?query=` | Менеджер, Керівник | Пошук: повний держномер або VIN (FR-18) | 200 `CarDto[]` | — |
| `GET /api/cars/{id}` | Менеджер, Керівник | Картка автомобіля, власник, активне замовлення | 200 `CarDetailsDto` | 404 `car_not_found` |
| `PUT /api/cars/{id}` | Менеджер | Редагування (FR-05) | 200 `CarDto` | 400, 404, 409 `vin_exists` |
| `GET /api/employees/masters` | Менеджер | Працівники з роллю «Майстер» (FR-08) | 200 `EmployeeDto[]` | — |
| `POST /api/orders` `{carId, clientRequest, works: [{category, description, estimatedCost, masterId?}]}` | Менеджер | Оформити замовлення (FR-06 – FR-08) | 201 `OrderDetailsDto` | 400 `validation_failed` (поля `works[i].*`); 404 `car_not_found`; 409 `active_order_exists` (`details: [номер]`) |
| `GET /api/orders/current?status=&masterId=` | Менеджер, Керівник | Поточні замовлення (FR-16) | 200 `OrderSummaryDto[]` | 400 (статус не поточний) |
| `GET /api/orders/completed?from=&to=` | Менеджер, Керівник | Завершені за період (FR-17) | 200 `OrderSummaryDto[]` | 400 (`to` < `from`) |
| `GET /api/orders/{id}` | Менеджер, Керівник | Картка замовлення (FR-19) | 200 `OrderDetailsDto` | 404 `order_not_found` |
| `POST /api/orders/{id}/works` `WorkRequest` | Менеджер | Додати роботу (FR-07) | 201 `OrderDetailsDto` | 400; 404; 409 `order_locked` |
| `PUT /api/works/{workId}` `WorkRequest` | Менеджер | Змінити заплановану роботу (FR-07) | 200 `OrderDetailsDto` | 400; 404; 409 `work_not_planned`, `order_locked` |
| `DELETE /api/works/{workId}` | Менеджер | Видалити заплановану роботу (FR-07) | 200 `OrderDetailsDto` | 404; 409 `work_not_planned`, `last_work`, `order_locked` |
| `PUT /api/works/{workId}/master` `{masterId}` | Менеджер | Призначити майстра (FR-08) | 200 `OrderDetailsDto` | 400 (не майстер); 404; 409 `work_not_planned`, `order_locked` |
| `POST /api/orders/{id}/start-work` | Менеджер | «Нове» → «В роботі» (FR-09) | 200 `OrderDetailsDto` | 404; 409 `masters_not_assigned` (`details`: роботи), `invalid_transition` |
| `POST /api/orders/{id}/mark-ready` | Менеджер | «В роботі» → «Готове до видачі» (FR-12) | 200 | 409 `works_not_done` (`details`), `invalid_transition` |
| `POST /api/orders/{id}/issue` | Менеджер | «Готове до видачі» → «Видане» (FR-13) | 200 | 409 `invalid_transition` |
| `POST /api/orders/{id}/cancel` `{reason}` | Менеджер | Скасування (FR-14) | 200 | 400 `validation_failed` (`reason`); 409 `invalid_transition` |
| `POST /api/works/{workId}/parts` `{name, quantity, expectedAt?}` | Менеджер | Додати деталь (FR-20) | 201 `OrderDetailsDto` | 400; 404; 409 `order_locked` |
| `POST /api/parts/{partId}/receive` | Менеджер | Відмітити надходження (FR-20) | 200 `OrderDetailsDto` | 404; 409 `part_already_received`, `order_locked` |
| `GET /api/master/works` | Майстер | Мої роботи в замовленнях «В роботі» (FR-10) | 200 `MasterWorkDto[]` | — |
| `POST /api/master/works/{workId}/advance` | Майстер | Наступний статус роботи (FR-11) | 200 `MasterWorkDto` | 403 `not_your_work`; 404; 409 `order_not_in_progress`, `work_already_done` |
| `GET /api/tracking?number=&phone=` | без входу | Відстеження клієнтом (FR-21) | 200 `TrackingDto` | 404 `order_not_found` (однаково для чужого телефону й неіснуючого номера) |
| `GET /health` | без входу | Контроль запуску | 200 `{"status":"ok"}` | — |

Типи повідомлень визначено в [Contracts.cs](../backend/src/TuningShop.Api/Contracts/Contracts.cs) і продубльовано для клієнта в [frontend/lib/types.ts](../frontend/lib/types.ts).

## Приклад: оформлення замовлення

```http
POST /api/orders
Content-Type: application/json

{ "carId": 1, "clientRequest": "Спортивний вигляд",
  "works": [ { "category": "Exhaust", "description": "Спортивний вихлоп", "estimatedCost": 18000, "masterId": 2 },
             { "category": "Lighting", "description": "LED-фари", "estimatedCost": 9000 } ] }
```

Відповідь `201 Created` містить `OrderDetailsDto` (`number`: `TS-2026-0001`, `status`: `New`, `history` з першим записом).
Якщо для автомобіля вже є активне замовлення, сервер повертає `409 {"code":"active_order_exists","message":"Для автомобіля вже є активне замовлення TS-2026-0001.","details":["TS-2026-0001"]}`.

## Помилки

Усі відмови мають тіло `ApiError`:

```json
{ "code": "validation_failed", "message": "Дані містять помилки. Виправте позначені поля.",
  "fields": { "phone": "Телефон має формат +380XXXXXXXXX." }, "details": null }
```

| Статус | Значення |
| --- | --- |
| 400 | Некоректні дані (`validation_failed` з `fields`), некоректна форма запиту чи JSON (`invalid_request`), невірні облікові дані |
| 401 | Потрібен вхід (`unauthorized`) |
| 403 | Функція недоступна для ролі (`forbidden`) або чужа робота (`not_your_work`) |
| 404 | Запис або маршрут не знайдено |
| 409 | Порушення предметного правила; дані не змінено |
| 500 / 503 | Внутрішня помилка (`internal_error`) / запис не підтверджено сховищем (`storage_unavailable`) — без внутрішніх деталей |
