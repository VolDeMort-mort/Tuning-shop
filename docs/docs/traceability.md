# Матриця простежуваності (RTM) v1.0

[← Назад: Проєктування](design.md) | [Далі: План реалізації →](implementation-plan.md)

| Вимога | Use Case | Критерій приймання | Проєктний артефакт |
| --- | --- | --- | --- |
| FR-01 Вхід за роллю | Передумова всіх UC, крім UC-10 | AC-01 | Use Case Diagram (актори); Class Diagram: Employee, Role |
| FR-02 Зареєструвати клієнта | UC-01 | AC-02 | Use Case Diagram; Class Diagram: Client |
| FR-03 Редагувати клієнта | UC-01 | AC-03 | Class Diagram: Client.updateContacts() |
| FR-04 Додати автомобіль | UC-02 | AC-04 | Use Case Diagram; Class Diagram: Car, Client–Car |
| FR-05 Редагувати автомобіль | UC-02 | AC-05 | Class Diagram: Car |
| FR-06 Створити замовлення | UC-03 | AC-06 | Activity UC-03; Class Diagram: Order, Car.hasActiveOrder(); State Machine: початковий перехід |
| FR-07 Керувати роботами | UC-03 | AC-07 | Activity UC-03; Class Diagram: WorkItem, WorkCategory; State Machine: внутрішні дії «Нове», «В роботі» |
| FR-08 Призначити майстра | UC-03 | AC-08 | Activity UC-03; Class Diagram: WorkItem–Employee (0..1) |
| FR-09 Передати в роботу | UC-04 | AC-09 | Activity UC-04; State Machine: «Нове» → «В роботі» |
| FR-10 Перелік робіт майстра | UC-05 | AC-10 | Activity UC-05; Class Diagram: WorkItem–Employee |
| FR-11 Змінити статус роботи | UC-05 | AC-11 | Activity UC-05; Class Diagram: WorkStatus, WorkItem.advanceStatus() |
| FR-12 Позначити готовим | UC-04 | AC-12 | Activity UC-04; Activity UC-05 (перевірка умови UC-04); State Machine: «В роботі» → «Готове до видачі»; Use Case Diagram («include» UC-05 → UC-04) |
| FR-13 Видати автомобіль | UC-04 | AC-13 | Activity UC-04; State Machine: «Готове до видачі» → «Видане»; Order.issuedAt |
| FR-14 Скасувати замовлення | UC-04 | AC-14 | Activity UC-04; State Machine: переходи в «Скасоване»; Order.cancelReason |
| FR-15 Історія статусів | UC-04 | AC-15 | Activity UC-04; Class Diagram: StatusChange |
| FR-16 Поточні замовлення | UC-06 | AC-16 | Use Case Diagram; Class Diagram: OrderStatus |
| FR-17 Завершені замовлення | UC-07 | AC-17 | Use Case Diagram; State Machine: фінальні стани |
| FR-18 Пошук і перегляд клієнта та автомобіля | UC-08, UC-11 | AC-18 | Use Case Diagram («include» UC-03 → UC-11); Activity UC-03, крок 1 |
| FR-19 Картка замовлення | UC-06, UC-07 | AC-19 | Class Diagram: Order.progress(), totalEstimate(), WorkItem–Part; Activity UC-05 |
| FR-20 Облік деталей для робіт | UC-09 | AC-27 | Use Case Diagram («include» UC-05 → UC-09, UC-09 → UC-10); Activity UC-05 (перегляд деталей роботи); Class Diagram: Part, PartStatus, WorkItem.addPart(), Part.markReceived(); State Machine: внутрішні дії «Нове», «В роботі» |
| FR-21 Відстеження стану клієнтом | UC-10 | AC-28 | Use Case Diagram (актор Клієнт); Class Diagram: Order.status, Order.progress(), WorkItem.status, Part.status, Client.phone |
| NFR-01 Час відповіді | UC-06, UC-08, UC-11 | AC-20 | План перевірки в ЛР №4 (етап 7) |
| NFR-02 Безпека | Усі UC | AC-21 | Class Diagram: Employee.role; план перевірки в ЛР №4 |
| NFR-03 Коректність даних | UC-01, UC-02, UC-03 | AC-22 | Activity UC-03 (A3) |
| NFR-04 Зручність | UC-03 | AC-23 | План перевірки в ЛР №4 (етап 7) |
| NFR-05 Сумісність | Усі UC | AC-24 | План перевірки в ЛР №4 (етап 7) |
| NFR-06 Збереженість даних | — (експлуатаційна вимога, не пов’язана зі сценарієм користувача) | AC-25 | План реалізації, етап 7 |
| NFR-07 Обмеження реалізації | — (обмеження на реалізацію, стосується системи в цілому) | AC-26 | План реалізації, етап 0 |

## Приклад простежування

Приклад простежування FR-09 «Передати в роботу». Вимога походить із функції v1.0 «Встановлення та зміна статусу замовлення» та з проблеми «складно визначити, хто відповідає за виконання робіт». Вона реалізується у UC-04 «Змінити статус замовлення» на кроці 4 основного сценарію та в альтернативі A3. На Activity Diagram UC-04 їй відповідає рішення «Майстри призначені всім роботам?», на State Machine Diagram — сторожова умова переходу «Нове» → «В роботі», а на Class Diagram — зв’язок WorkItem–Employee з кратністю 0..1, через який перевіряється наявність майстра. Перевіряється вимога критерієм AC-09. У ЛР №4 цей ланцюг буде доповнено посиланням на код і результат тесту.

---

[← Назад: Проєктування](design.md) | [Далі: План реалізації →](implementation-plan.md)
