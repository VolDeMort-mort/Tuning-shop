namespace TuningShop.Api.Domain;

/// <summary>Запис історії змін статусу замовлення (FR-15).</summary>
public sealed class StatusChange
{
    private StatusChange() { }

    public StatusChange(OrderStatus? fromStatus, OrderStatus toStatus, int employeeId, DateTime changedAt, string? comment)
    {
        FromStatus = fromStatus;
        ToStatus = toStatus;
        EmployeeId = employeeId;
        ChangedAt = changedAt;
        Comment = comment;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public OrderStatus? FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public int EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }
    public string? Comment { get; private set; }
}
