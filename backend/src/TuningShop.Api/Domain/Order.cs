namespace TuningShop.Api.Domain;

/// <summary>
/// Замовлення. Методи StartWork, MarkReady, Issue і Cancel відповідають переходам
/// State Machine Diagram ЛР №3; кожен успішний перехід додає рівно один запис історії (FR-15).
/// </summary>
public sealed class Order
{
    private Order() { }

    public int Id { get; private set; }
    public string Number { get; private set; } = "";
    public int CarId { get; private set; }
    public Car? Car { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string ClientRequest { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public DateTime? IssuedAt { get; private set; }
    public string? CancelReason { get; private set; }
    public List<WorkItem> WorkItems { get; private set; } = [];
    public List<StatusChange> StatusChanges { get; private set; } = [];

    /// <summary>Початковий перехід [*] → «Нове» [є хоча б одна робота] (FR-06).</summary>
    public static Order Create(int carId, string number, string clientRequest,
        IReadOnlyCollection<WorkItem> works, int employeeId, DateTime now)
    {
        if (works.Count == 0)
            throw new ArgumentException("Замовлення повинно містити щонайменше одну роботу.", nameof(works));

        var order = new Order
        {
            CarId = carId,
            Number = number,
            CreatedAt = now,
            ClientRequest = clientRequest.Trim(),
            Status = OrderStatus.New
        };
        order.WorkItems.AddRange(works);
        order.StatusChanges.Add(new StatusChange(null, OrderStatus.New, employeeId, now, null));
        return order;
    }

    /// <summary>Внутрішні дії станів «Нове» і «В роботі»: роботи та деталі (FR-07, FR-20).</summary>
    public bool AllowsWorkChanges => Status is OrderStatus.New or OrderStatus.InProgress;

    public int DoneCount => WorkItems.Count(w => w.Status == WorkStatus.Done);

    public bool AllWorksDone => WorkItems.Count > 0 && WorkItems.All(w => w.Status == WorkStatus.Done);

    /// <summary>Прогрес «виконано X з Y» (FR-19, FR-21).</summary>
    public string Progress() => $"виконано {DoneCount} з {WorkItems.Count}";

    public decimal TotalEstimate() => WorkItems.Sum(w => w.EstimatedCost);

    public DomainError? AddWork(WorkItem item)
    {
        if (!AllowsWorkChanges)
            return new DomainError("order_locked",
                "Додавати роботи можна лише до замовлення у статусі «Нове» або «В роботі».");
        WorkItems.Add(item);
        return null;
    }

    public DomainError? RemoveWork(WorkItem item)
    {
        if (!AllowsWorkChanges)
            return new DomainError("order_locked",
                "Змінювати роботи можна лише в замовленні у статусі «Нове» або «В роботі».");
        if (item.Status != WorkStatus.Planned)
            return new DomainError("work_not_planned", "Видалити можна лише роботу у статусі «Заплановано».");
        if (WorkItems.Count == 1)
            return new DomainError("last_work", "Не можна видалити останню роботу замовлення.");
        WorkItems.Remove(item);
        return null;
    }

    /// <summary>«Нове» → «В роботі» [усім роботам призначено майстра] (FR-09).</summary>
    public DomainError? StartWork(int employeeId, DateTime now)
    {
        if (Status != OrderStatus.New)
            return InvalidTransition("передати в роботу");
        var withoutMaster = WorkItems.Where(w => w.MasterId is null).Select(w => w.Description).ToList();
        if (withoutMaster.Count > 0)
            return new DomainError("masters_not_assigned",
                "Не всім роботам призначено майстра.", Details: withoutMaster);
        ChangeStatus(OrderStatus.InProgress, employeeId, now, null);
        return null;
    }

    /// <summary>«В роботі» → «Готове до видачі» [усі роботи «Виконано»] (FR-12).</summary>
    public DomainError? MarkReady(int employeeId, DateTime now)
    {
        if (Status != OrderStatus.InProgress)
            return InvalidTransition("позначити готовим");
        var unfinished = WorkItems.Where(w => w.Status != WorkStatus.Done).Select(w => w.Description).ToList();
        if (unfinished.Count > 0)
            return new DomainError("works_not_done", "Не всі роботи виконано.", Details: unfinished);
        ChangeStatus(OrderStatus.Ready, employeeId, now, null);
        return null;
    }

    /// <summary>«Готове до видачі» → «Видане» / зберегти дату видачі (FR-13).</summary>
    public DomainError? Issue(int employeeId, DateTime now)
    {
        if (Status != OrderStatus.Ready)
            return InvalidTransition("видати автомобіль");
        IssuedAt = now;
        ChangeStatus(OrderStatus.Issued, employeeId, now, null);
        return null;
    }

    /// <summary>«Нове» / «В роботі» → «Скасоване»(причина) (FR-14).</summary>
    public DomainError? Cancel(string? reason, int employeeId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return DomainError.Invalid("reason", "Вкажіть причину скасування.");
        if (Status is not (OrderStatus.New or OrderStatus.InProgress))
            return InvalidTransition("скасувати");
        CancelReason = reason.Trim();
        ChangeStatus(OrderStatus.Cancelled, employeeId, now, CancelReason);
        return null;
    }

    private void ChangeStatus(OrderStatus to, int employeeId, DateTime now, string? comment)
    {
        StatusChanges.Add(new StatusChange(Status, to, employeeId, now, comment));
        Status = to;
    }

    private DomainError InvalidTransition(string action) =>
        new("invalid_transition", $"Для замовлення у статусі «{StatusNames.Of(Status)}» не можна {action}.");
}
