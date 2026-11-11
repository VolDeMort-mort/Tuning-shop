using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;
using TuningShop.Tests.Support;
using static TuningShop.Tests.Support.Builders;

namespace TuningShop.Tests.Integration;

/// <summary>UC-05 «Виконати призначену роботу» і UC-09 облік деталей (AC-10, AC-11, AC-19, AC-27).</summary>
public sealed class MasterAndPartsTests : IAsyncLifetime
{
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task AC10_MasterSeesOnlyOwnWorks_FromInProgressOrders()
    {
        var active = await _db.InProgressOrderAsync(Work(_db.Master1.Id, "Моя"), Work(_db.Master2.Id, "Чужа"));
        var client = await _db.ClientAsync("Інший Клієнт", "+380671112233");
        var car = await _db.CarAsync(client.Id, "WBS8M9C50M5K12345", "KA7777AX");
        await _db.CreateOrderAsync(car.Id, Work(_db.Master1.Id, "Ще не в роботі")); // «Нове»

        await using var context = _db.NewContext();
        var works = await new MasterWorkService(context, _db.Clock).GetMyWorksAsync(_db.Master1.Id);

        var work = Assert.Single(works);
        Assert.Equal("Моя", work.Description);
        Assert.Equal(active.Number, work.OrderNumber);
        Assert.Equal("AA1234BB", work.PlateNumber);
        Assert.Equal(WorkStatus.Planned, work.Status);
    }

    [Fact]
    public async Task AC11_AdvanceOwnWork_TimeSaved_ProgressUpdated()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id, "Перша"), Work(_db.Master1.Id, "Друга"));
        var workId = order.Works[0].Id;
        _db.Clock.UtcNow = TestDatabase.Now.AddHours(1);

        await using (var context = _db.NewContext())
        {
            var service = new MasterWorkService(context, _db.Clock);
            Assert.Equal(WorkStatus.InProgress, (await service.AdvanceAsync(workId, _db.Master1.Id)).Value!.Status);
            var done = await service.AdvanceAsync(workId, _db.Master1.Id);
            Assert.Equal(WorkStatus.Done, done.Value!.Status);
            Assert.Equal("виконано 1 з 2", done.Value.OrderProgress);
        }

        var card = await _db.ReloadAsync(order.Id); // AC-19: прогрес у картці після оновлення
        Assert.Equal("виконано 1 з 2", card.Progress);
        Assert.Equal(TestDatabase.Now.AddHours(1), card.Works[0].StatusChangedAt);
    }

    [Fact]
    public async Task AC11_ForeignWork_OrderNotInProgress_OrBackwards_Rejected()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id));
        var workId = order.Works[0].Id;
        await using var context = _db.NewContext();
        var service = new MasterWorkService(context, _db.Clock);

        Assert.Equal("not_your_work", (await service.AdvanceAsync(workId, _db.Master2.Id)).Error?.Code);

        await service.AdvanceAsync(workId, _db.Master1.Id);
        await service.AdvanceAsync(workId, _db.Master1.Id);
        Assert.Equal("work_already_done", (await service.AdvanceAsync(workId, _db.Master1.Id)).Error?.Code);

        var cancelled = await _db.InProgressOrderAsync(Work(_db.Master1.Id));
        await using (var c2 = _db.NewContext())
            await new OrderStatusService(c2, _db.Clock).CancelAsync(cancelled.Id, new CancelOrderRequest("Відмова"), _db.Manager.Id);
        await using var c3 = _db.NewContext();
        var refused = await new MasterWorkService(c3, _db.Clock).AdvanceAsync(cancelled.Works[0].Id, _db.Master1.Id);
        Assert.Equal("order_not_in_progress", refused.Error?.Code);
        Assert.Equal(WorkStatus.Planned, (await _db.ReloadAsync(cancelled.Id)).Works[0].Status);
    }

    [Fact]
    public async Task UC05_Step9_AllWorksDone_FlagInCurrentList()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id));
        await _db.CompleteAllWorksAsync(order);

        await using var context = _db.NewContext();
        var current = Assert.Single((await new OrderQueryService(context).GetCurrentAsync(null, null)).Value!);
        Assert.True(current.AllWorksDone);
        Assert.Equal(OrderStatus.InProgress, current.Status); // статус змінює Менеджер (UC-04)
    }

    [Fact]
    public async Task AC27_Parts_AddAndMarkReceived_ValidationAndLockedStates()
    {
        var order = await _db.InProgressOrderAsync(Work(_db.Master1.Id));
        var workId = order.Works[0].Id;
        _db.Clock.UtcNow = TestDatabase.Now.AddDays(1);

        await using (var context = _db.NewContext())
        {
            var service = new PartService(context, _db.Clock);
            var added = await service.AddAsync(workId, new AddPartRequest("Койловери KW", 1, new DateOnly(2026, 10, 25)));
            var part = Assert.Single(added.Value!.Works[0].Parts);
            Assert.Equal(PartStatus.Awaiting, part.Status);
            Assert.False(added.Value.Works[0].AllPartsReceived);

            Assert.Equal(["name", "quantity"],
                (await service.AddAsync(workId, new AddPartRequest("", 0, null))).Error!.Fields!.Keys.Order());

            var received = await service.MarkReceivedAsync(part.Id);
            var stored = received.Value!.Works[0].Parts[0];
            Assert.Equal(PartStatus.Received, stored.Status);
            Assert.Equal(TestDatabase.Now.AddDays(1), stored.ReceivedAt);
            Assert.Equal("part_already_received", (await service.MarkReceivedAsync(part.Id)).Error?.Code);
        }

        // Майстер бачить деталі своєї роботи (UC-05 крок 3).
        await using (var context = _db.NewContext())
            Assert.Single(Assert.Single(await new MasterWorkService(context, _db.Clock).GetMyWorksAsync(_db.Master1.Id)).Parts);

        await _db.CompleteAllWorksAsync(order);
        await using (var context = _db.NewContext())
            await new OrderStatusService(context, _db.Clock).MarkReadyAsync(order.Id, _db.Manager.Id);
        await using var after = _db.NewContext();
        var locked = await new PartService(after, _db.Clock).AddAsync(workId, new AddPartRequest("Ще", 1, null));
        Assert.Equal("order_locked", locked.Error?.Code);
    }
}
