namespace TuningShop.Api.Domain;

/// <summary>Деталь, потрібна для роботи (FR-20).</summary>
public sealed class Part
{
    private Part() { }

    /// <summary>Дані вже перевірено <see cref="Validation.ValidatePart"/>.</summary>
    public Part(string name, int quantity, DateOnly? expectedAt)
    {
        Name = name.Trim();
        Quantity = quantity;
        ExpectedAt = expectedAt;
        Status = PartStatus.Awaiting;
    }

    public int Id { get; private set; }
    public int WorkItemId { get; private set; }
    public WorkItem? WorkItem { get; private set; }
    public string Name { get; private set; } = "";
    public int Quantity { get; private set; }
    public PartStatus Status { get; private set; }
    public DateOnly? ExpectedAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }

    public DomainError? MarkReceived(DateTime now)
    {
        if (Status == PartStatus.Received)
            return new DomainError("part_already_received", "Надходження цієї деталі вже відмічено.");
        Status = PartStatus.Received;
        ReceivedAt = now;
        return null;
    }
}
