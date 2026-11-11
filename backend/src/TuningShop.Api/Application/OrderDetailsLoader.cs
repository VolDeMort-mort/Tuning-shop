using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;

namespace TuningShop.Api.Application;

internal static class OrderDetailsLoader
{
    /// <summary>Повторне читання картки після підтвердженого запису: відповідь відображає збережений стан.</summary>
    public static async Task<OrderDetailsDto> LoadOrderDetailsAsync(this TuningDbContext db, int orderId)
    {
        db.ChangeTracker.Clear();
        var order = await db.Orders.AsNoTracking().WithDetails().FirstAsync(o => o.Id == orderId);
        return order.ToDetails();
    }
}
