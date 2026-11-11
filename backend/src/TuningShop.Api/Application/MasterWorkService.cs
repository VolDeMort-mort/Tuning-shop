using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-05 «Виконати призначену роботу» (FR-10, FR-11).</summary>
public sealed class MasterWorkService(TuningDbContext db, TimeProvider clock)
{
    /// <summary>Роботи майстра лише із замовлень «В роботі» (FR-10, AC-10).</summary>
    public async Task<IReadOnlyList<MasterWorkDto>> GetMyWorksAsync(int masterId)
    {
        // Відстежуваний запит: Include Order → WorkItems утворює цикл, недопустимий для AsNoTracking.
        var works = await db.WorkItems
            .Where(w => w.MasterId == masterId && w.Order!.Status == OrderStatus.InProgress)
            .Include(w => w.Parts)
            .Include(w => w.Order!).ThenInclude(o => o.Car)
            .Include(w => w.Order!).ThenInclude(o => o.WorkItems)
            .AsSplitQuery()
            .OrderBy(w => w.OrderId).ThenBy(w => w.Id)
            .ToListAsync();
        return works.Select(ToDto).ToList();
    }

    /// <summary>Кроки 5–8 UC-05 і альтернативи A2, A3.</summary>
    public async Task<Result<MasterWorkDto>> AdvanceAsync(int workId, int masterId)
    {
        var work = await db.WorkItems
            .Include(w => w.Parts)
            .Include(w => w.Order!).ThenInclude(o => o.Car)
            .Include(w => w.Order!).ThenInclude(o => o.WorkItems)
            .FirstOrDefaultAsync(w => w.Id == workId);
        if (work is null)
            return DomainError.NotFound("work_not_found", "Роботу не знайдено.");
        if (work.MasterId != masterId)
            return new DomainError("not_your_work", "Ця робота призначена іншому майстру.", ErrorKind.Forbidden);
        if (work.Order!.Status != OrderStatus.InProgress)
            return new DomainError("order_not_in_progress",
                $"Замовлення має статус «{StatusNames.Of(work.Order.Status)}»; змінювати роботи можна лише для замовлення «В роботі».");

        if (work.Advance(clock.GetUtcNow().UtcDateTime) is { } error)
            return error;

        await db.SaveChangesAsync();
        // Крок 9: якщо всі роботи виконано, замовлення позначається у списку поточних
        // (OrderSummaryDto.AllWorksDone), а статус змінює Менеджер (UC-04).
        return ToDto(work);
    }

    private static MasterWorkDto ToDto(WorkItem w)
    {
        var order = w.Order!;
        return new(w.Id, order.Id, order.Number, order.Car!.Title(), order.Car!.PlateNumber,
            w.Category, w.Description, w.Status, StatusNames.Of(w.Status), w.StatusChangedAt,
            order.Progress(), w.Parts.OrderBy(p => p.Id).Select(p => p.ToDto()).ToList());
    }
}
