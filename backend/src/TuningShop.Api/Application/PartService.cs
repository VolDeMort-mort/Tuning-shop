using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-09 «Вести облік деталей для робіт» (FR-20).</summary>
public sealed class PartService(TuningDbContext db, TimeProvider clock)
{
    public async Task<Result<OrderDetailsDto>> AddAsync(int workId, AddPartRequest request)
    {
        var work = await db.WorkItems.Include(w => w.Order).FirstOrDefaultAsync(w => w.Id == workId);
        if (work is null) return DomainError.NotFound("work_not_found", "Роботу не знайдено.");
        if (!work.Order!.AllowsWorkChanges) return PartsLocked();

        var errors = Validation.ValidatePart(request.Name, request.Quantity);
        if (errors.Count > 0) return DomainError.Invalid(errors);

        work.Parts.Add(new Part(request.Name!, request.Quantity!.Value, request.ExpectedAt));
        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(work.OrderId);
    }

    public async Task<Result<OrderDetailsDto>> MarkReceivedAsync(int partId)
    {
        var part = await db.Parts.Include(p => p.WorkItem!).ThenInclude(w => w.Order)
            .FirstOrDefaultAsync(p => p.Id == partId);
        if (part is null) return DomainError.NotFound("part_not_found", "Деталь не знайдено.");
        if (!part.WorkItem!.Order!.AllowsWorkChanges) return PartsLocked();

        if (part.MarkReceived(clock.GetUtcNow().UtcDateTime) is { } error) return error;
        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(part.WorkItem.OrderId);
    }

    private static DomainError PartsLocked() => new("order_locked",
        "Змінювати деталі можна лише в замовленні у статусі «Нове» або «В роботі».");
}
