using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;
using TuningShop.Tests.Support;
using static TuningShop.Tests.Support.Builders;

namespace TuningShop.Tests.Integration;

/// <summary>UC-04 «Змінити статус замовлення» зі збереженням у SQLite (AC-09, AC-12 – AC-15).</summary>
public sealed class OrderStatusServiceTests : IAsyncLifetime
{
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<Result<OrderDetailsDto>> Act(Func<OrderStatusService, Task<Result<OrderDetailsDto>>> action)
    {
        await using var context = _db.NewContext();
        return await action(new OrderStatusService(context, _db.Clock));
    }

    [Fact]
    public async Task AC09_StartWork_AllMastersAssigned_StatusChanged()
    {
        var order = await _db.NewOrderAsync(Work(_db.Master1.Id), Work(_db.Master2.Id, "Друга"));
        var result = await Act(s => s.StartWorkAsync(order.Id, _db.Manager.Id));

        Assert.Equal(OrderStatus.InProgress, result.Value!.Status);
        Assert.Equal(OrderStatus.InProgress, (await _db.ReloadAsync(order.Id)).Status);
    }

    [Fact]
    public async Task AC09_StartWork_WorkWithoutMaster_NotChanged_ListShown()
    {
        var order = await _db.NewOrderAsync(Work(_db.Master1.Id), Work(null, "Без майстра"));
        var result = await Act(s => s.StartWorkAsync(order.Id, _db.Manager.Id));

        Assert.Equal("masters_not_assigned", result.Error?.Code);
        Assert.Equal(["Без майстра"], result.Error!.Details);
        var stored = await _db.ReloadAsync(order.Id);
        Assert.Equal(OrderStatus.New, stored.Status);
        Assert.Single(stored.History);
    }

    [Fact]
    public async Task AC12_MarkReady_OnlyWhenAllDone()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id, "Перша"), Work(_db.Master2.Id, "Друга"));

        var early = await Act(s => s.MarkReadyAsync(order.Id, _db.Manager.Id));
        Assert.Equal("works_not_done", early.Error?.Code);
        Assert.Equal(["Перша", "Друга"], early.Error!.Details);

        await _db.CompleteAllWorksAsync(order);
        var ready = await Act(s => s.MarkReadyAsync(order.Id, _db.Manager.Id));
        Assert.Equal(OrderStatus.Ready, ready.Value!.Status);
    }

    [Fact]
    public async Task AC13_Issue_SavesDate_MovesToCompleted_NoFurtherChanges()
    {
        var order = await _db.InProgressOrderAsync();
        await _db.CompleteAllWorksAsync(order);
        await Act(s => s.MarkReadyAsync(order.Id, _db.Manager.Id));
        _db.Clock.UtcNow = TestDatabase.Now.AddDays(2);

        var issued = await Act(s => s.IssueAsync(order.Id, _db.Manager.Id));
        Assert.Equal(OrderStatus.Issued, issued.Value!.Status);
        Assert.Equal(TestDatabase.Now.AddDays(2), issued.Value.IssuedAt);
        Assert.Empty(issued.Value.AllowedActions);
        Assert.False(issued.Value.CanEditWorks);

        await using var context = _db.NewContext();
        var queries = new OrderQueryService(context);
        Assert.Empty((await queries.GetCurrentAsync(null, null)).Value!);
        Assert.Single((await queries.GetCompletedAsync(null, null)).Value!);
        Assert.Equal("invalid_transition", (await Act(s => s.CancelAsync(order.Id, new CancelOrderRequest("x"), _db.Manager.Id))).Error?.Code);
    }

    [Fact]
    public async Task AC14_Cancel_RequiresReason_NotAllowedAfterReady()
    {
        var order = await _db.InProgressOrderAsync();
        Assert.Equal("validation_failed", (await Act(s => s.CancelAsync(order.Id, new CancelOrderRequest(""), _db.Manager.Id))).Error?.Code);

        var cancelled = await Act(s => s.CancelAsync(order.Id, new CancelOrderRequest("Клієнт відмовився"), _db.Manager.Id));
        Assert.Equal(OrderStatus.Cancelled, cancelled.Value!.Status);
        Assert.Equal("Клієнт відмовився", cancelled.Value.CancelReason);

        var ready = await _db.InProgressOrderAsync();
        await _db.CompleteAllWorksAsync(ready);
        await Act(s => s.MarkReadyAsync(ready.Id, _db.Manager.Id));
        var refused = await Act(s => s.CancelAsync(ready.Id, new CancelOrderRequest("Пізно"), _db.Manager.Id));
        Assert.Equal("invalid_transition", refused.Error?.Code);
        Assert.Equal(OrderStatus.Ready, (await _db.ReloadAsync(ready.Id)).Status);
    }

    [Fact]
    public async Task AC15_EachSuccessfulChange_AddsExactlyOneHistoryRecord()
    {
        var order = await _db.InProgressOrderAsync();
        await Act(s => s.StartWorkAsync(order.Id, _db.Manager.Id)); // відхилено: вже «В роботі»
        await _db.CompleteAllWorksAsync(order);
        await Act(s => s.MarkReadyAsync(order.Id, _db.Manager.Id));
        await Act(s => s.IssueAsync(order.Id, _db.Manager.Id));

        var history = (await _db.ReloadAsync(order.Id)).History;
        Assert.Equal([OrderStatus.New, OrderStatus.InProgress, OrderStatus.Ready, OrderStatus.Issued],
            history.Select(h => h.ToStatus));
        Assert.Equal([null, OrderStatus.New, OrderStatus.InProgress, OrderStatus.Ready],
            history.Select(h => h.FromStatus));
        Assert.All(history, h => Assert.Equal(_db.Manager.FullName, h.EmployeeName));
        Assert.Equal(4, await _db.NewContext().StatusChanges.CountAsync());
    }

    [Fact]
    public async Task UnknownOrder_NotFound() =>
        Assert.Equal("order_not_found", (await Act(s => s.StartWorkAsync(999, _db.Manager.Id))).Error?.Code);
}
