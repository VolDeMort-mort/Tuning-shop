namespace TuningShop.Api.Domain;

public sealed class WorkItem
{
    private WorkItem() { }

    /// <summary>Дані вже перевірено <see cref="Validation.ValidateWork"/>. Нова робота має статус «Заплановано» (FR-07).</summary>
    public WorkItem(WorkCategory category, string description, decimal estimatedCost, DateTime now)
    {
        Category = category;
        Description = description.Trim();
        EstimatedCost = estimatedCost;
        Status = WorkStatus.Planned;
        StatusChangedAt = now;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public Order? Order { get; private set; }
    public WorkCategory Category { get; private set; }
    public string Description { get; private set; } = "";
    public decimal EstimatedCost { get; private set; }
    public WorkStatus Status { get; private set; }
    public DateTime StatusChangedAt { get; private set; }
    public int? MasterId { get; private set; }
    public Employee? Master { get; private set; }
    public List<Part> Parts { get; private set; } = [];

    public bool AllPartsReceived() => Parts.All(p => p.Status == PartStatus.Received);

    /// <summary>Редагувати можна лише заплановану роботу (FR-07).</summary>
    public DomainError? Update(WorkCategory category, string description, decimal estimatedCost)
    {
        if (Status != WorkStatus.Planned)
            return new DomainError("work_not_planned", "Змінити можна лише роботу у статусі «Заплановано».");
        Category = category;
        Description = description.Trim();
        EstimatedCost = estimatedCost;
        return null;
    }

    /// <summary>Призначити або змінити майстра лише для запланованої роботи (FR-08).</summary>
    public DomainError? AssignMaster(Employee master)
    {
        if (master.Role != Role.Master)
            return DomainError.Invalid("masterId", "Обраний працівник не має ролі «Майстер».");
        if (Status != WorkStatus.Planned)
            return new DomainError("work_not_planned", "Змінити майстра можна лише для роботи у статусі «Заплановано».");
        MasterId = master.Id;
        Master = master;
        return null;
    }

    /// <summary>Перехід лише вперед: «Заплановано» → «Виконується» → «Виконано» (FR-11).</summary>
    public DomainError? Advance(DateTime now)
    {
        if (Status == WorkStatus.Done)
            return new DomainError("work_already_done", "Робота вже виконана; повернення до попереднього статусу не передбачено.");
        Status = Status == WorkStatus.Planned ? WorkStatus.InProgress : WorkStatus.Done;
        StatusChangedAt = now;
        return null;
    }
}
