using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Data;

public static class QueryExtensions
{
    /// <summary>Усе, що потрібно для картки замовлення (FR-19).</summary>
    public static IQueryable<Order> WithDetails(this IQueryable<Order> orders) =>
        orders
            .Include(o => o.Car!).ThenInclude(c => c.Client)
            .Include(o => o.WorkItems).ThenInclude(w => w.Master)
            .Include(o => o.WorkItems).ThenInclude(w => w.Parts)
            .Include(o => o.StatusChanges).ThenInclude(s => s.Employee)
            .AsSplitQuery();

    /// <summary>Дані для рядка списку замовлень.</summary>
    public static IQueryable<Order> WithSummary(this IQueryable<Order> orders) =>
        orders
            .Include(o => o.Car!).ThenInclude(c => c.Client)
            .Include(o => o.WorkItems)
            .AsSplitQuery();
}
