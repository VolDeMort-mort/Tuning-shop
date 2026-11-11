using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Data;

/// <summary>
/// Тестова база для демонстрації та приймання: замовлення в усіх статусах State Machine,
/// а також генератор обсягу для перевірки NFR-01.
/// </summary>
public static class DemoData
{
    public static async Task SeedDemoAsync(TuningDbContext db, DateTime now)
    {
        if (await db.Clients.AnyAsync())
            throw new InvalidOperationException("База вже містить клієнтів; демонстраційні дані додаються лише до порожньої бази.");

        var manager = await db.Employees.FirstAsync(e => e.Role == Role.Manager);
        var masters = await db.Employees.Where(e => e.Role == Role.Master).OrderBy(e => e.Id).ToListAsync();

        var bondar = new Client("Бондар Андрій Петрович", "+380501112233", "bondar@example.com");
        var lysenko = new Client("Лисенко Олена Іванівна", "+380671234567", null);
        var melnyk = new Client("Мельник Тарас Сергійович", "+380931112244", null);
        db.Clients.AddRange(bondar, lysenko, melnyk);
        await db.SaveChangesAsync();

        var golf = new Car(bondar.Id, "Volkswagen", "Golf GTI", 2019, "WVWZZZAUZKW123456", "AA1234BB");
        var bmw = new Car(bondar.Id, "BMW", "M3", 2021, "WBS8M9C50M5K12345", "KA7777AX");
        var subaru = new Car(lysenko.Id, "Subaru", "WRX STI", 2018, "JF1VA2M67J9812345", "BC4321EK");
        var audi = new Car(melnyk.Id, "Audi", "A4", 2017, "WAUZZZF41HA123456", "AI0001CC");
        db.Cars.AddRange(golf, bmw, subaru, audi);
        await db.SaveChangesAsync();

        WorkItem Work(WorkCategory category, string description, decimal cost, Employee? master, DateTime at)
        {
            var w = new WorkItem(category, description, cost, at);
            if (master is not null) w.AssignMaster(master);
            return w;
        }

        // «Нове»: одна робота ще без майстра.
        var newOrder = Order.Create(golf.Id, "TS-DEMO-0001", "Хоче агресивніший вигляд і гучніший вихлоп",
        [
            Work(WorkCategory.BodyKit, "Встановлення переднього спліттера", 8500m, masters[0], now.AddHours(-3)),
            Work(WorkCategory.Exhaust, "Заміна глушника на спортивний", 21000m, null, now.AddHours(-3))
        ], manager.Id, now.AddHours(-3));

        // «В роботі»: одна робота виконана, друга виконується; деталі очікуються й отримані.
        var suspension = Work(WorkCategory.Suspension, "Встановлення койловерів", 32000m, masters[0], now.AddDays(-2));
        suspension.Parts.Add(new Part("Койловери KW V3", 1, DateOnly.FromDateTime(now.AddDays(2))));
        var chip = Work(WorkCategory.ChipTuning, "Чип-тюнінг Stage 1", 12000m, masters[1], now.AddDays(-2));
        var inProgress = Order.Create(subaru.Id, "TS-DEMO-0002", "Покращити керованість", [suspension, chip],
            manager.Id, now.AddDays(-2));
        inProgress.StartWork(manager.Id, now.AddDays(-2).AddHours(1));
        chip.Advance(now.AddDays(-1));
        chip.Advance(now.AddDays(-1).AddHours(2));
        suspension.Advance(now.AddHours(-5));
        var gasket = new Part("Прокладки кріплення", 4, null);
        suspension.Parts.Add(gasket);
        gasket.MarkReceived(now.AddDays(-1));

        // «Готове до видачі».
        var lighting = Work(WorkCategory.Lighting, "Встановлення LED-фар", 15000m, masters[1], now.AddDays(-6));
        var ready = Order.Create(bmw.Id, "TS-DEMO-0003", "Сучасне світло", [lighting], manager.Id, now.AddDays(-6));
        ready.StartWork(manager.Id, now.AddDays(-6));
        lighting.Advance(now.AddDays(-5));
        lighting.Advance(now.AddDays(-4));
        ready.MarkReady(manager.Id, now.AddDays(-4));

        // «Видане».
        var detailing = Work(WorkCategory.Detailing, "Полірування і кераміка", 9000m, masters[0], now.AddDays(-20));
        var issued = Order.Create(audi.Id, "TS-DEMO-0004", "Детейлінг перед продажем", [detailing], manager.Id, now.AddDays(-20));
        issued.StartWork(manager.Id, now.AddDays(-20));
        detailing.Advance(now.AddDays(-19));
        detailing.Advance(now.AddDays(-18));
        issued.MarkReady(manager.Id, now.AddDays(-18));
        issued.Issue(manager.Id, now.AddDays(-17));

        // «Скасоване».
        var cancelled = Order.Create(audi.Id, "TS-DEMO-0005", "Тонування",
            [Work(WorkCategory.Other, "Тонування задніх стекол", 3000m, null, now.AddDays(-10))], manager.Id, now.AddDays(-10));
        cancelled.Cancel("Клієнт відмовився від послуги", manager.Id, now.AddDays(-9));

        db.Orders.AddRange(newOrder, inProgress, ready, issued, cancelled);
        await db.SaveChangesAsync();
    }

    /// <summary>Набір обсягом <paramref name="orderCount"/> замовлень для AC-20 (NFR-01).</summary>
    public static async Task SeedVolumeAsync(TuningDbContext db, int orderCount, DateTime now)
    {
        var manager = await db.Employees.FirstAsync(e => e.Role == Role.Manager);
        var masters = await db.Employees.Where(e => e.Role == Role.Master).ToListAsync();
        var random = new Random(42);
        var categories = Enum.GetValues<WorkCategory>();
        const string vinChars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        var startPhone = 380990000000L + await db.Clients.CountAsync();

        db.ChangeTracker.AutoDetectChangesEnabled = false;
        for (var batch = 0; batch < orderCount; batch += 500)
        {
            var size = Math.Min(500, orderCount - batch);
            var clients = new List<Client>();
            for (var i = 0; i < size; i++)
                clients.Add(new Client($"Клієнт Тестовий {batch + i:D5}", $"+{startPhone + batch + i}", null));
            db.Clients.AddRange(clients);
            await db.SaveChangesAsync();

            var cars = clients.Select(c => new Car(c.Id, "Toyota", "Supra", 2015 + random.Next(10),
                new string(Enumerable.Range(0, 17).Select(_ => vinChars[random.Next(vinChars.Length)]).ToArray()),
                $"PF{c.Id:D6}")).ToList();
            db.Cars.AddRange(cars);
            await db.SaveChangesAsync();

            foreach (var car in cars)
            {
                var created = now.AddDays(-random.Next(1, 365));
                var works = Enumerable.Range(0, 1 + random.Next(3))
                    .Select(_ =>
                    {
                        var w = new WorkItem(categories[random.Next(categories.Length)], "Типова робота", 1000m * random.Next(1, 30), created);
                        w.AssignMaster(masters[random.Next(masters.Count)]);
                        return w;
                    }).ToList();
                var order = Order.Create(car.Id, $"TS-PERF-{car.Id:D6}", "Згенеровано для NFR-01", works, manager.Id, created);
                // Близько 20 % замовлень поточні, решта — видані.
                if (random.Next(5) > 0)
                {
                    order.StartWork(manager.Id, created);
                    foreach (var w in works) { w.Advance(created); w.Advance(created); }
                    order.MarkReady(manager.Id, created);
                    order.Issue(manager.Id, created.AddDays(1));
                }
                db.Orders.Add(order);
            }
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            db.Employees.AttachRange(masters);
        }
        db.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
