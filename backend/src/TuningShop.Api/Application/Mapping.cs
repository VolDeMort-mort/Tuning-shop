using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>Перетворення предметних класів на об'єкти передавання даних.</summary>
public static class Mapping
{
    public static ClientDto ToDto(this Client c) => new(c.Id, c.FullName, c.Phone, c.Email);

    public static CarDto ToDto(this Car c) => new(c.Id, c.ClientId, c.Make, c.Model, c.Year, c.Vin, c.PlateNumber);

    public static string Title(this Car c) => $"{c.Make} {c.Model} ({c.Year})";

    public static EmployeeDto ToDto(this Employee e) => new(e.Id, e.FullName, e.Role);

    public static PartDto ToDto(this Part p) =>
        new(p.Id, p.Name, p.Quantity, p.Status, StatusNames.Of(p.Status), p.ExpectedAt, p.ReceivedAt);

    public static WorkItemDto ToDto(this WorkItem w) =>
        new(w.Id, w.Category, w.Description, w.EstimatedCost, w.Status, StatusNames.Of(w.Status),
            w.StatusChangedAt, w.MasterId, w.Master?.FullName, w.AllPartsReceived(),
            w.Parts.OrderBy(p => p.Id).Select(ToDto).ToList());

    /// <summary>Потребує Car.Client і WorkItems (<see cref="Data.QueryExtensions.WithSummary"/>).</summary>
    public static OrderSummaryDto ToSummary(this Order o)
    {
        var car = o.Car!;
        return new(o.Id, o.Number, o.CreatedAt, o.Status, StatusNames.Of(o.Status),
            car.Id, car.Title(), car.PlateNumber, car.ClientId, car.Client!.FullName,
            o.DoneCount, o.WorkItems.Count, o.Progress(), o.AllWorksDone, o.IssuedAt, o.CancelReason);
    }

    /// <summary>Потребує повного завантаження (<see cref="Data.QueryExtensions.WithDetails"/>).</summary>
    public static OrderDetailsDto ToDetails(this Order o)
    {
        var car = o.Car!;
        return new(o.Id, o.Number, o.CreatedAt, o.ClientRequest, o.Status, StatusNames.Of(o.Status),
            o.IssuedAt, o.CancelReason, car.Client!.ToDto(), car.ToDto(),
            o.WorkItems.OrderBy(w => w.Id).Select(ToDto).ToList(),
            o.DoneCount, o.WorkItems.Count, o.Progress(), o.AllWorksDone, o.TotalEstimate(),
            o.AllowsWorkChanges, AllowedActions(o.Status),
            o.StatusChanges.OrderBy(s => s.ChangedAt).ThenBy(s => s.Id).Select(s => new StatusChangeDto(
                s.ChangedAt, s.FromStatus, s.FromStatus is { } from ? StatusNames.Of(from) : null,
                s.ToStatus, StatusNames.Of(s.ToStatus), s.Employee?.FullName ?? "", s.Comment)).ToList());
    }

    /// <summary>Дії, дозволені State Machine Diagram для поточного статусу.</summary>
    public static IReadOnlyList<string> AllowedActions(OrderStatus status) => status switch
    {
        OrderStatus.New => ["startWork", "cancel"],
        OrderStatus.InProgress => ["markReady", "cancel"],
        OrderStatus.Ready => ["issue"],
        _ => []
    };
}
