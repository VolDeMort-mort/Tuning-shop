using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>
/// UC-04 «Змінити статус замовлення». Усі переходи State Machine виконує <see cref="Order"/>;
/// служба завантажує замовлення, викликає перехід і підтверджує запис (FR-09, FR-12 – FR-15).
/// </summary>
public sealed class OrderStatusService(TuningDbContext db, TimeProvider clock)
{
    public Task<Result<OrderDetailsDto>> StartWorkAsync(int orderId, int employeeId) =>
        ChangeAsync(orderId, (order, now) => order.StartWork(employeeId, now));

    public Task<Result<OrderDetailsDto>> MarkReadyAsync(int orderId, int employeeId) =>
        ChangeAsync(orderId, (order, now) => order.MarkReady(employeeId, now));

    public Task<Result<OrderDetailsDto>> IssueAsync(int orderId, int employeeId) =>
        ChangeAsync(orderId, (order, now) => order.Issue(employeeId, now));

    public Task<Result<OrderDetailsDto>> CancelAsync(int orderId, CancelOrderRequest request, int employeeId) =>
        ChangeAsync(orderId, (order, now) => order.Cancel(request.Reason, employeeId, now));

    private async Task<Result<OrderDetailsDto>> ChangeAsync(int orderId, Func<Order, DateTime, DomainError?> transition)
    {
        var order = await db.Orders
            .Include(o => o.WorkItems)
            .Include(o => o.StatusChanges)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
            return DomainError.NotFound("order_not_found", "Замовлення не знайдено.");

        // A3: умову переходу не виконано — статус та історія не змінюються, запис не виконується.
        if (transition(order, clock.GetUtcNow().UtcDateTime) is { } error)
            return error;

        await db.SaveChangesAsync();
        return await db.LoadOrderDetailsAsync(order.Id);
    }
}
