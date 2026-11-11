using TuningShop.Api.Domain;

namespace TuningShop.Tests.Domain;

/// <summary>
/// Модульні перевірки State Machine Diagram замовлення: кожен дозволений і недозволений перехід
/// (FR-09, FR-12 – FR-15) та правила робіт FR-07, FR-08, FR-11.
/// </summary>
public sealed class OrderStateMachineTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private const int ManagerId = 1;

    private static Employee Master() => new("Майстер", "m", "h", Role.Master);

    private static (Order Order, WorkItem Work) NewOrder(bool withMaster = true)
    {
        var work = new WorkItem(WorkCategory.Exhaust, "Вихлоп", 100m, T0);
        if (withMaster) work.AssignMaster(Master());
        return (Order.Create(1, "TS-2026-0001", "", [work], ManagerId, T0), work);
    }

    private static Order OrderIn(OrderStatus status)
    {
        var (order, work) = NewOrder();
        if (status == OrderStatus.New) return order;
        if (status == OrderStatus.Cancelled) { order.Cancel("Причина", ManagerId, T0); return order; }
        order.StartWork(ManagerId, T0);
        if (status == OrderStatus.InProgress) return order;
        work.Advance(T0); work.Advance(T0);
        order.MarkReady(ManagerId, T0);
        if (status == OrderStatus.Ready) return order;
        order.Issue(ManagerId, T0);
        return order;
    }

    [Fact]
    public void Create_StartsAsNew_WithFirstHistoryRecord()
    {
        var (order, _) = NewOrder();
        Assert.Equal(OrderStatus.New, order.Status);
        var first = Assert.Single(order.StatusChanges);
        Assert.Null(first.FromStatus);
        Assert.Equal(OrderStatus.New, first.ToStatus);
    }

    [Fact]
    public void Create_WithoutWorks_IsNotAllowed() =>
        Assert.Throws<ArgumentException>(() => Order.Create(1, "N", "", [], ManagerId, T0));

    // Матриця переходів: (поточний статус, дія) → очікуваний новий статус або null, якщо перехід заборонено.
    public static TheoryData<OrderStatus, string, OrderStatus?> Transitions => new()
    {
        { OrderStatus.New, "start", OrderStatus.InProgress },
        { OrderStatus.New, "ready", null },
        { OrderStatus.New, "issue", null },
        { OrderStatus.New, "cancel", OrderStatus.Cancelled },
        { OrderStatus.InProgress, "start", null },
        { OrderStatus.InProgress, "ready", OrderStatus.Ready },
        { OrderStatus.InProgress, "issue", null },
        { OrderStatus.InProgress, "cancel", OrderStatus.Cancelled },
        { OrderStatus.Ready, "start", null },
        { OrderStatus.Ready, "ready", null },
        { OrderStatus.Ready, "issue", OrderStatus.Issued },
        { OrderStatus.Ready, "cancel", null },
        { OrderStatus.Issued, "start", null },
        { OrderStatus.Issued, "ready", null },
        { OrderStatus.Issued, "issue", null },
        { OrderStatus.Issued, "cancel", null },
        { OrderStatus.Cancelled, "start", null },
        { OrderStatus.Cancelled, "ready", null },
        { OrderStatus.Cancelled, "issue", null },
        { OrderStatus.Cancelled, "cancel", null },
    };

    [Theory]
    [MemberData(nameof(Transitions))]
    public void Transition_FollowsStateMachine(OrderStatus from, string action, OrderStatus? expected)
    {
        var order = OrderIn(from);
        if (from == OrderStatus.InProgress && action == "ready")
            foreach (var w in order.WorkItems) { w.Advance(T0); w.Advance(T0); }
        var historyBefore = order.StatusChanges.Count;

        var error = action switch
        {
            "start" => order.StartWork(ManagerId, T0),
            "ready" => order.MarkReady(ManagerId, T0),
            "issue" => order.Issue(ManagerId, T0),
            _ => order.Cancel("Клієнт відмовився", ManagerId, T0)
        };

        if (expected is null)
        {
            Assert.NotNull(error);
            Assert.Equal(from, order.Status);
            Assert.Equal(historyBefore, order.StatusChanges.Count); // AC-15: відхилена спроба запису не додає
        }
        else
        {
            Assert.Null(error);
            Assert.Equal(expected, order.Status);
            Assert.Equal(historyBefore + 1, order.StatusChanges.Count); // AC-15: рівно один запис
            var last = order.StatusChanges[^1];
            Assert.Equal(from, last.FromStatus);
            Assert.Equal(expected, last.ToStatus);
            Assert.Equal(ManagerId, last.EmployeeId);
        }
    }

    [Fact]
    public void StartWork_WithoutMaster_ListsWorks() // AC-09
    {
        var (order, _) = NewOrder(withMaster: false);
        var error = order.StartWork(ManagerId, T0);
        Assert.Equal("masters_not_assigned", error?.Code);
        Assert.Equal(["Вихлоп"], error!.Details);
        Assert.Equal(OrderStatus.New, order.Status);
    }

    [Fact]
    public void MarkReady_WithUnfinishedWork_ListsWorks() // AC-12
    {
        var order = OrderIn(OrderStatus.InProgress);
        var error = order.MarkReady(ManagerId, T0);
        Assert.Equal("works_not_done", error?.Code);
        Assert.Equal(["Вихлоп"], error!.Details);
    }

    [Fact]
    public void Issue_StoresIssueDate() // AC-13
    {
        var order = OrderIn(OrderStatus.Ready);
        var at = T0.AddDays(3);
        Assert.Null(order.Issue(ManagerId, at));
        Assert.Equal(at, order.IssuedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Cancel_WithoutReason_IsRejected(string? reason) // AC-14
    {
        var (order, _) = NewOrder();
        Assert.Equal("validation_failed", order.Cancel(reason, ManagerId, T0)?.Code);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Single(order.StatusChanges);
    }

    [Fact]
    public void Work_AdvancesOnlyForward() // AC-11
    {
        var work = new WorkItem(WorkCategory.Other, "Робота", 0m, T0);
        Assert.Null(work.Advance(T0.AddHours(1)));
        Assert.Equal(WorkStatus.InProgress, work.Status);
        Assert.Null(work.Advance(T0.AddHours(2)));
        Assert.Equal(WorkStatus.Done, work.Status);
        Assert.Equal(T0.AddHours(2), work.StatusChangedAt);
        Assert.Equal("work_already_done", work.Advance(T0.AddHours(3))?.Code);
    }

    [Fact]
    public void Work_EditAndMasterChange_OnlyWhenPlanned() // AC-07, AC-08
    {
        var work = new WorkItem(WorkCategory.Other, "Робота", 0m, T0);
        Assert.Null(work.Update(WorkCategory.Lighting, "Нова", 5m));
        work.Advance(T0);
        Assert.Equal("work_not_planned", work.Update(WorkCategory.Other, "Ще", 1m)?.Code);
        Assert.Equal("work_not_planned", work.AssignMaster(Master())?.Code);
    }

    [Fact]
    public void AssignMaster_RequiresMasterRole() // FR-08
    {
        var work = new WorkItem(WorkCategory.Other, "Робота", 0m, T0);
        var manager = new Employee("Менеджер", "mgr", "h", Role.Manager);
        Assert.Equal("validation_failed", work.AssignMaster(manager)?.Code);
        Assert.Null(work.MasterId);
    }

    [Fact]
    public void RemoveWork_KeepsAtLeastOneWork() // AC-07
    {
        var (order, work) = NewOrder();
        Assert.Equal("last_work", order.RemoveWork(work)?.Code);
        var second = new WorkItem(WorkCategory.Other, "Друга", 0m, T0);
        Assert.Null(order.AddWork(second));
        Assert.Null(order.RemoveWork(second));
        Assert.Single(order.WorkItems);
    }

    [Fact]
    public void Progress_And_TotalEstimate() // FR-19
    {
        var a = new WorkItem(WorkCategory.Other, "A", 100.50m, T0);
        var b = new WorkItem(WorkCategory.Other, "B", 200m, T0);
        var order = Order.Create(1, "N", "", [a, b], ManagerId, T0);
        a.Advance(T0); a.Advance(T0);
        Assert.Equal("виконано 1 з 2", order.Progress());
        Assert.Equal(300.50m, order.TotalEstimate());
        Assert.False(order.AllWorksDone);
    }
}
