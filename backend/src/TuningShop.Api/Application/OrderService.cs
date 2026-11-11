using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-03 «Оформити замовлення»: створення замовлення, роботи й майстри (FR-06, FR-07, FR-08).</summary>
public sealed class OrderService(TuningDbContext db, TimeProvider clock)
{
    /// <summary>Основний сценарій UC-03, кроки 4–11; альтернативи A2 і A3.</summary>
    public async Task<Result<OrderDetailsDto>> CreateAsync(CreateOrderRequest request, int employeeId)
    {
        if (request.CarId is null)
            return DomainError.Invalid("carId", "Оберіть автомобіль.");
        var works = request.Works ?? [];
        if (works.Count == 0)
            return DomainError.Invalid("works", "Додайте щонайменше одну роботу.");

        var car = await db.Cars.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == request.CarId);
        if (car is null)
            return DomainError.NotFound("car_not_found", "Автомобіль не знайдено.");

        // A2: для автомобіля вже є активне замовлення — нове не створюється.
        if (car.ActiveOrder() is { } active)
            return new DomainError("active_order_exists",
                $"Для автомобіля вже є активне замовлення {active.Number}.", Details: [active.Number]);

        // A3: дані робіт перевіряються до збереження; помилки позначаються біля полів.
        var fieldErrors = new Dictionary<string, string>();
        for (var i = 0; i < works.Count; i++)
            foreach (var (field, message) in Validation.ValidateWork(works[i].Category, works[i].Description, works[i].EstimatedCost))
                fieldErrors[$"works[{i}].{field}"] = message;
        if (fieldErrors.Count > 0)
            return DomainError.Invalid(fieldErrors);

        var masters = await LoadMastersAsync(works.Select(w => w.MasterId));
        var now = clock.GetUtcNow().UtcDateTime;
        var items = new List<WorkItem>();
        for (var i = 0; i < works.Count; i++)
        {
            var w = works[i];
            var item = new WorkItem(w.Category!.Value, w.Description!, w.EstimatedCost!.Value, now);
            // A4: робота без майстра допускається; передати замовлення в роботу не вдасться до призначення (FR-09).
            if (w.MasterId is { } masterId)
            {
                if (!masters.TryGetValue(masterId, out var master))
                    return DomainError.Invalid($"works[{i}].masterId", "Майстра не знайдено.");
                if (item.AssignMaster(master) is { } error)
                    return error with { Fields = new Dictionary<string, string> { [$"works[{i}].masterId"] = error.Message } };
            }
            items.Add(item);
        }

        var order = Order.Create(car.Id, await NextNumberAsync(now), request.ClientRequest ?? "", items, employeeId, now);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(order.Id);
    }

    /// <summary>Додати роботу до замовлення «Нове» або «В роботі» (FR-07, AC-07).</summary>
    public async Task<Result<OrderDetailsDto>> AddWorkAsync(int orderId, WorkRequest request)
    {
        var order = await db.Orders.Include(o => o.WorkItems).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return OrderNotFound();

        var errors = Validation.ValidateWork(request.Category, request.Description, request.EstimatedCost);
        if (errors.Count > 0) return DomainError.Invalid(errors);

        var item = new WorkItem(request.Category!.Value, request.Description!, request.EstimatedCost!.Value,
            clock.GetUtcNow().UtcDateTime);
        if (request.MasterId is { } masterId)
        {
            var master = await db.Employees.FindAsync(masterId);
            if (master is null) return DomainError.Invalid("masterId", "Майстра не знайдено.");
            if (item.AssignMaster(master) is { } assignError) return assignError;
        }
        if (order.AddWork(item) is { } error) return error;

        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(order.Id);
    }

    /// <summary>Змінити заплановану роботу (FR-07).</summary>
    public async Task<Result<OrderDetailsDto>> UpdateWorkAsync(int workId, WorkRequest request)
    {
        var work = await db.WorkItems.Include(w => w.Order).FirstOrDefaultAsync(w => w.Id == workId);
        if (work is null) return WorkNotFound();
        if (!work.Order!.AllowsWorkChanges) return OrderLocked();

        var errors = Validation.ValidateWork(request.Category, request.Description, request.EstimatedCost);
        if (errors.Count > 0) return DomainError.Invalid(errors);
        if (work.Update(request.Category!.Value, request.Description!, request.EstimatedCost!.Value) is { } error)
            return error;

        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(work.OrderId);
    }

    /// <summary>Видалити заплановану роботу, не залишаючи замовлення без робіт (FR-07).</summary>
    public async Task<Result<OrderDetailsDto>> DeleteWorkAsync(int workId)
    {
        var work = await db.WorkItems.FirstOrDefaultAsync(w => w.Id == workId);
        if (work is null) return WorkNotFound();
        var order = await db.Orders.Include(o => o.WorkItems).FirstAsync(o => o.Id == work.OrderId);

        if (order.RemoveWork(work) is { } error) return error;
        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(order.Id);
    }

    /// <summary>Призначити або змінити майстра запланованої роботи (FR-08, AC-08).</summary>
    public async Task<Result<OrderDetailsDto>> AssignMasterAsync(int workId, AssignMasterRequest request)
    {
        if (request.MasterId is null) return DomainError.Invalid("masterId", "Оберіть майстра.");
        var work = await db.WorkItems.Include(w => w.Order).FirstOrDefaultAsync(w => w.Id == workId);
        if (work is null) return WorkNotFound();
        if (!work.Order!.AllowsWorkChanges) return OrderLocked();

        var master = await db.Employees.FindAsync(request.MasterId.Value);
        if (master is null) return DomainError.Invalid("masterId", "Майстра не знайдено.");
        if (work.AssignMaster(master) is { } error) return error;

        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(work.OrderId);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetMastersAsync()
    {
        var masters = await db.Employees.AsNoTracking().Where(e => e.Role == Role.Master)
            .OrderBy(e => e.FullName).ToListAsync();
        return masters.Select(m => m.ToDto()).ToList();
    }

    /// <summary>Унікальний номер вигляду TS-2026-0001 (послідовність у межах року).</summary>
    private async Task<string> NextNumberAsync(DateTime now)
    {
        var prefix = $"TS-{now.Year}-";
        var last = await db.Orders.Where(o => o.Number.StartsWith(prefix))
            .OrderByDescending(o => o.Number).Select(o => o.Number).FirstOrDefaultAsync();
        var next = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;
        return $"{prefix}{next:D4}";
    }

    private async Task<Dictionary<int, Employee>> LoadMastersAsync(IEnumerable<int?> ids)
    {
        var wanted = ids.OfType<int>().Distinct().ToList();
        return await db.Employees.Where(e => wanted.Contains(e.Id)).ToDictionaryAsync(e => e.Id);
    }

    private static DomainError OrderNotFound() => DomainError.NotFound("order_not_found", "Замовлення не знайдено.");
    private static DomainError WorkNotFound() => DomainError.NotFound("work_not_found", "Роботу не знайдено.");
    private static DomainError OrderLocked() => new("order_locked",
        "Змінювати роботи можна лише в замовленні у статусі «Нове» або «В роботі».");
}
