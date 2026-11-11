using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;
using TuningShop.Tests.Support;
using static TuningShop.Tests.Support.Builders;

namespace TuningShop.Tests.Integration;

/// <summary>UC-06, UC-07, UC-10: списки, картка й відстеження клієнтом (AC-16, AC-17, AC-19, AC-28).</summary>
public sealed class QueryAndTrackingTests : IAsyncLifetime
{
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<OrderDetailsDto> OrderForCar(string phone, string vin, string plate, params WorkRequest[] works)
    {
        var client = await _db.ClientAsync("Клієнт " + plate, phone);
        var car = await _db.CarAsync(client.Id, vin, plate);
        return (await _db.CreateOrderAsync(car.Id, works)).Value!;
    }

    [Fact]
    public async Task AC16_CurrentList_AllAndOnlyCurrent_FiltersByStatusAndMaster()
    {
        var a = await OrderForCar("+380500000001", "AAAAAAAAAAAAAAAA1", "AA0001AA", Work(_db.Master1.Id));
        var b = await OrderForCar("+380500000002", "AAAAAAAAAAAAAAAA2", "AA0002AA", Work(_db.Master2.Id));
        var c = await OrderForCar("+380500000003", "AAAAAAAAAAAAAAAA3", "AA0003AA", Work(_db.Master1.Id));
        await using (var context = _db.NewContext())
        {
            var statuses = new OrderStatusService(context, _db.Clock);
            await statuses.StartWorkAsync(b.Id, _db.Manager.Id);
            await statuses.CancelAsync(c.Id, new CancelOrderRequest("Відмова"), _db.Manager.Id);
        }

        await using var verify = _db.NewContext();
        var queries = new OrderQueryService(verify);
        Assert.Equal([a.Id, b.Id], (await queries.GetCurrentAsync(null, null)).Value!.Select(o => o.Id).Order());
        Assert.Equal([b.Id], (await queries.GetCurrentAsync(OrderStatus.InProgress, null)).Value!.Select(o => o.Id));
        Assert.Equal([a.Id], (await queries.GetCurrentAsync(null, _db.Master1.Id)).Value!.Select(o => o.Id));
        Assert.Equal("validation_failed", (await queries.GetCurrentAsync(OrderStatus.Issued, null)).Error?.Code);
    }

    [Fact]
    public async Task AC17_CompletedList_FilteredByPeriod_WithIssueDateOrCancelReason()
    {
        var issued = await OrderForCar("+380500000001", "AAAAAAAAAAAAAAAA1", "AA0001AA", Work(_db.Master1.Id));
        var cancelled = await OrderForCar("+380500000002", "AAAAAAAAAAAAAAAA2", "AA0002AA", Work(_db.Master1.Id));
        await OrderForCar("+380500000003", "AAAAAAAAAAAAAAAA3", "AA0003AA", Work(_db.Master1.Id)); // поточне

        await using (var context = _db.NewContext())
        {
            var statuses = new OrderStatusService(context, _db.Clock);
            _db.Clock.UtcNow = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
            await statuses.CancelAsync(cancelled.Id, new CancelOrderRequest("Клієнт передумав"), _db.Manager.Id);
            var started = (await statuses.StartWorkAsync(issued.Id, _db.Manager.Id)).Value!;
            await _db.CompleteAllWorksAsync(started);
            await statuses.MarkReadyAsync(issued.Id, _db.Manager.Id);
            _db.Clock.UtcNow = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
            await statuses.IssueAsync(issued.Id, _db.Manager.Id);
        }

        await using var verify = _db.NewContext();
        var queries = new OrderQueryService(verify);
        var all = (await queries.GetCompletedAsync(null, null)).Value!;
        Assert.Equal(2, all.Count);
        Assert.Equal("Клієнт передумав", all.Single(o => o.Id == cancelled.Id).CancelReason);
        Assert.NotNull(all.Single(o => o.Id == issued.Id).IssuedAt);

        Assert.Equal([cancelled.Id], (await queries.GetCompletedAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5))).Value!.Select(o => o.Id));
        Assert.Equal([issued.Id], (await queries.GetCompletedAsync(new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 15))).Value!.Select(o => o.Id));
        Assert.Empty((await queries.GetCompletedAsync(new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 14))).Value!);
        Assert.Equal("validation_failed", (await queries.GetCompletedAsync(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1))).Error?.Code);
    }

    [Fact]
    public async Task AC19_Card_MatchesStoredData()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id, "Перша", 1000m), Work(_db.Master2.Id, "Друга", 250.50m));
        await using (var context = _db.NewContext())
            await new PartService(context, _db.Clock).AddAsync(order.Works[1].Id, new AddPartRequest("Фари", 2, null));

        var card = await _db.ReloadAsync(order.Id);
        Assert.Equal("Бондар Андрій Петрович", card.Client.FullName);
        Assert.Equal("AA1234BB", card.Car.PlateNumber);
        Assert.Equal([_db.Master1.FullName, _db.Master2.FullName], card.Works.Select(w => w.MasterName));
        Assert.Equal("Фари", Assert.Single(card.Works[1].Parts).Name);
        Assert.Equal("виконано 0 з 2", card.Progress);
        Assert.Equal(1250.50m, card.TotalEstimate);
        Assert.Equal(2, card.History.Count);
        Assert.Equal(["markReady", "cancel"], card.AllowedActions);
    }

    [Fact]
    public async Task AC28_Tracking_CorrectPairShowsState_OtherwiseNotFound()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id, "Вихлоп"));
        await using (var context = _db.NewContext())
            await new PartService(context, _db.Clock).AddAsync(order.Works[0].Id, new AddPartRequest("Глушник", 1, null));
        var other = await OrderForCar("+380671112233", "WBS8M9C50M5K12345", "KA7777AX", Work(_db.Master1.Id));

        await using var verify = _db.NewContext();
        var tracking = new TrackingService(verify);

        var shown = (await tracking.TrackAsync(order.Number.ToLowerInvariant(), "+380 50 111 22 33")).Value!;
        Assert.Equal(order.Number, shown.Number);
        Assert.Equal(OrderStatus.InProgress, shown.Status);
        Assert.Equal("виконано 0 з 1", shown.Progress);
        var part = Assert.Single(Assert.Single(shown.Works).Parts);
        Assert.Equal(PartStatus.Awaiting, part.Status);

        // Чужий телефон, неіснуючий номер і порожні поля дають однакову відмову без даних.
        Assert.Equal("order_not_found", (await tracking.TrackAsync(order.Number, "+380671112233")).Error?.Code);
        Assert.Equal("order_not_found", (await tracking.TrackAsync(other.Number, "+380501112233")).Error?.Code);
        Assert.Equal("order_not_found", (await tracking.TrackAsync("TS-0000-0000", "+380501112233")).Error?.Code);
        Assert.Equal("order_not_found", (await tracking.TrackAsync(null, null)).Error?.Code);
    }
}
