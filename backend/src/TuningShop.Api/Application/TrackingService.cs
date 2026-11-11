using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-10 «Відстежити стан замовлення і деталей» — без входу, лише перегляд (FR-21).</summary>
public sealed class TrackingService(TuningDbContext db)
{
    public async Task<Result<TrackingDto>> TrackAsync(string? number, string? phone)
    {
        var orderNumber = (number ?? "").Trim().ToUpperInvariant();
        var ownerPhone = Validation.NormalizePhone(phone);

        var order = orderNumber.Length == 0 || ownerPhone.Length == 0
            ? null
            : await db.Orders.AsNoTracking()
                .Include(o => o.Car)
                .Include(o => o.WorkItems).ThenInclude(w => w.Parts)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Number == orderNumber && o.Car!.Client!.Phone == ownerPhone);

        // Однакова відповідь для неіснуючого номера й чужого телефону: дані не розкриваються (NFR-02).
        if (order is null)
            return DomainError.NotFound("order_not_found", "Замовлення не знайдено.");

        return new TrackingDto(order.Number, order.Car!.Title(), order.Car!.PlateNumber,
            order.Status, StatusNames.Of(order.Status), order.DoneCount, order.WorkItems.Count, order.Progress(),
            order.WorkItems.OrderBy(w => w.Id).Select(w => new TrackingWorkDto(
                w.Category, w.Description, w.Status, StatusNames.Of(w.Status),
                w.Parts.OrderBy(p => p.Id).Select(p => new TrackingPartDto(
                    p.Name, p.Quantity, p.Status, StatusNames.Of(p.Status), p.ExpectedAt, p.ReceivedAt)).ToList()))
            .ToList());
    }
}
