using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;
using TuningShop.Tests.Support;
using static TuningShop.Tests.Support.Builders;

namespace TuningShop.Tests.Integration;

/// <summary>UC-03 «Оформити замовлення» за всіма гілками Activity UC-03 (AC-06, AC-07, AC-08).</summary>
public sealed class OrderServiceTests : IAsyncLifetime
{
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task AC06_OrderSaved_WithNumberDateStatusAndHistory_AppearsInCurrent()
    {
        var order = await _db.NewOrderAsync(Work(_db.Master1.Id, cost: 1500m), Work(null, "Тонування", 500m));

        Assert.Equal($"TS-{TestDatabase.Now.Year}-0001", order.Number);
        Assert.Equal(TestDatabase.Now, order.CreatedAt);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(2000m, order.TotalEstimate);
        Assert.All(order.Works, w => Assert.Equal(WorkStatus.Planned, w.Status));
        var history = Assert.Single(order.History);
        Assert.Equal(OrderStatus.New, history.ToStatus);
        Assert.Equal(_db.Manager.FullName, history.EmployeeName);

        await using var context = _db.NewContext();
        var current = (await new OrderQueryService(context).GetCurrentAsync(null, null)).Value!;
        Assert.Equal(order.Number, Assert.Single(current).Number);
    }

    [Fact]
    public async Task AC06_SecondActiveOrder_NotCreated_ShowsActiveNumber() // A2
    {
        var first = await _db.NewOrderAsync();
        var second = await _db.CreateOrderAsync(first.Car.Id, Work(_db.Master1.Id));

        Assert.Equal("active_order_exists", second.Error?.Code);
        Assert.Equal([first.Number], second.Error!.Details);
        Assert.Equal(1, await _db.NewContext().Orders.CountAsync());
    }

    [Fact]
    public async Task AC06_AfterCancel_NewOrderAllowed_WithNextNumber()
    {
        var first = await _db.NewOrderAsync();
        await using (var context = _db.NewContext())
            await new OrderStatusService(context, _db.Clock).CancelAsync(first.Id, new CancelOrderRequest("Відмова"), _db.Manager.Id);

        var second = await _db.CreateOrderAsync(first.Car.Id, Work(_db.Master1.Id));
        Assert.True(second.IsSuccess);
        Assert.Equal($"TS-{TestDatabase.Now.Year}-0002", second.Value!.Number);
    }

    [Fact]
    public async Task AC07_InvalidWork_MarkedByField_NothingSaved() // A3
    {
        var client = await _db.ClientAsync();
        var car = await _db.CarAsync(client.Id);

        var result = await _db.CreateOrderAsync(car.Id,
            Work(_db.Master1.Id), new WorkRequest(null, "", -5m, null));

        Assert.Equal("validation_failed", result.Error?.Code);
        Assert.Equal(["works[1].category", "works[1].description", "works[1].estimatedCost"],
            result.Error!.Fields!.Keys.Order());
        Assert.Equal(0, await _db.NewContext().Orders.CountAsync());

        var empty = await _db.CreateOrderAsync(car.Id);
        Assert.Equal("works", Assert.Single(empty.Error!.Fields!).Key);
    }

    [Fact]
    public async Task AC07_AddWork_OnlyForNewOrInProgress()
    {
        var order = await _db.NewOrderAsync();
        await using (var context = _db.NewContext())
        {
            var added = await new OrderService(context, _db.Clock).AddWorkAsync(order.Id, Work(null, "Друга"));
            Assert.Equal(2, added.Value!.WorkCount);
            Assert.Equal(WorkStatus.Planned, added.Value.Works[^1].Status);
        }

        await using (var context = _db.NewContext())
            await new OrderStatusService(context, _db.Clock).CancelAsync(order.Id, new CancelOrderRequest("Відмова"), _db.Manager.Id);

        await using var after = _db.NewContext();
        var locked = await new OrderService(after, _db.Clock).AddWorkAsync(order.Id, Work(null, "Третя"));
        Assert.Equal("order_locked", locked.Error?.Code);
        Assert.Equal(2, (await _db.ReloadAsync(order.Id)).WorkCount);
    }

    [Fact]
    public async Task AC07_StartedOrDoneWork_CannotBeEditedOrDeleted_LastWorkCannotBeDeleted()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id, "Перша"), Work(_db.Master1.Id, "Друга"));
        var first = order.Works[0];
        var second = order.Works[1];
        await using (var context = _db.NewContext())
            await new MasterWorkService(context, _db.Clock).AdvanceAsync(first.Id, _db.Master1.Id);

        await using var context2 = _db.NewContext();
        var service = new OrderService(context2, _db.Clock);
        Assert.Equal("work_not_planned", (await service.UpdateWorkAsync(first.Id, Work(null, "Змінена"))).Error?.Code);
        Assert.Equal("work_not_planned", (await service.DeleteWorkAsync(first.Id)).Error?.Code);

        var edited = await service.UpdateWorkAsync(second.Id, Work(null, "Друга змінена", 777m, WorkCategory.Lighting));
        Assert.Equal("Друга змінена", edited.Value!.Works[1].Description);

        var single = await _db.NewOrderAsync();
        await using var context3 = _db.NewContext();
        Assert.Equal("last_work", (await new OrderService(context3, _db.Clock).DeleteWorkAsync(single.Works[0].Id)).Error?.Code);
    }

    [Fact]
    public async Task AC08_AssignMaster_OnlyMasterRole_OnlyPlannedWork()
    {
        var order = await _db.NewOrderAsync(Work(null));
        var workId = order.Works[0].Id;
        await using var context = _db.NewContext();
        var service = new OrderService(context, _db.Clock);

        var notMaster = await service.AssignMasterAsync(workId, new AssignMasterRequest(_db.Manager.Id));
        Assert.Equal("validation_failed", notMaster.Error?.Code);

        var assigned = await service.AssignMasterAsync(workId, new AssignMasterRequest(_db.Master2.Id));
        Assert.Equal(_db.Master2.FullName, assigned.Value!.Works[0].MasterName);

        var changed = await service.AssignMasterAsync(workId, new AssignMasterRequest(_db.Master1.Id));
        Assert.Equal(_db.Master1.Id, changed.Value!.Works[0].MasterId);

        // Майстер бачить роботу, щойно замовлення перейде в «В роботі».
        await using var c2 = _db.NewContext();
        Assert.Empty(await new MasterWorkService(c2, _db.Clock).GetMyWorksAsync(_db.Master1.Id));
        await new OrderStatusService(c2, _db.Clock).StartWorkAsync(order.Id, _db.Manager.Id);
        await using var c3 = _db.NewContext();
        Assert.Single(await new MasterWorkService(c3, _db.Clock).GetMyWorksAsync(_db.Master1.Id));

        // Для роботи, що виконується, зміна майстра недоступна.
        await new MasterWorkService(c3, _db.Clock).AdvanceAsync(workId, _db.Master1.Id);
        await using var c4 = _db.NewContext();
        var late = await new OrderService(c4, _db.Clock).AssignMasterAsync(workId, new AssignMasterRequest(_db.Master2.Id));
        Assert.Equal("work_not_planned", late.Error?.Code);
    }

    [Fact]
    public async Task A4_WorkWithoutMaster_IsSaved_ButCannotStartWork()
    {
        var order = await _db.NewOrderAsync(Work(null, "Без майстра"));
        Assert.Null(order.Works[0].MasterId);

        await using var context = _db.NewContext();
        var start = await new OrderStatusService(context, _db.Clock).StartWorkAsync(order.Id, _db.Manager.Id);
        Assert.Equal("masters_not_assigned", start.Error?.Code);
    }
}
