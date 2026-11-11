using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-06, UC-07: поточні й завершені замовлення та картка замовлення (FR-16, FR-17, FR-19).</summary>
public sealed class OrderQueryService(TuningDbContext db)
{
    /// <summary>Поточні замовлення з фільтрами за статусом і майстром (FR-16, AC-16).</summary>
    public async Task<Result<IReadOnlyList<OrderSummaryDto>>> GetCurrentAsync(OrderStatus? status, int? masterId)
    {
        if (status is { } s && !OrderStatuses.Current.Contains(s))
            return DomainError.Invalid("status", "Фільтр за статусом допускає лише поточні статуси.");

        var query = db.Orders.AsNoTracking().Where(o => OrderStatuses.Current.Contains(o.Status));
        if (status is not null) query = query.Where(o => o.Status == status);
        if (masterId is not null) query = query.Where(o => o.WorkItems.Any(w => w.MasterId == masterId));

        var orders = await query.WithSummary().OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(o => o.ToSummary()).ToList();
    }

    /// <summary>
    /// Завершені замовлення за період [from; to] у днях (FR-17, AC-17). Для виданого замовлення
    /// використовується дата видачі, для скасованого — дата скасування з історії статусів.
    /// </summary>
    public async Task<Result<IReadOnlyList<OrderSummaryDto>>> GetCompletedAsync(DateOnly? from, DateOnly? to)
    {
        if (from is not null && to is not null && from > to)
            return DomainError.Invalid("to", "Кінець періоду не може бути раніше за початок.");

        var start = (from ?? DateOnly.MinValue).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to is { } t ? t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : DateTime.MaxValue;

        var orders = await db.Orders.AsNoTracking()
            .Where(o =>
                (o.Status == OrderStatus.Issued && o.IssuedAt >= start && o.IssuedAt < end) ||
                (o.Status == OrderStatus.Cancelled && o.StatusChanges.Any(c =>
                    c.ToStatus == OrderStatus.Cancelled && c.ChangedAt >= start && c.ChangedAt < end)))
            .WithSummary()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return orders.Select(o => o.ToSummary()).ToList();
    }

    public async Task<Result<OrderDetailsDto>> GetDetailsAsync(int id)
    {
        var order = await db.Orders.AsNoTracking().WithDetails().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return DomainError.NotFound("order_not_found", "Замовлення не знайдено.");
        return order.ToDetails();
    }
}
