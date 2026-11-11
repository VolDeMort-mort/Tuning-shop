namespace TuningShop.Api.Domain;

/// <summary>Статус замовлення (State Machine Diagram ЛР №3).</summary>
public enum OrderStatus
{
    New,        // «Нове»
    InProgress, // «В роботі»
    Ready,      // «Готове до видачі»
    Issued,     // «Видане»
    Cancelled   // «Скасоване»
}

/// <summary>Статус роботи: лише вперед Planned → InProgress → Done (FR-11).</summary>
public enum WorkStatus
{
    Planned,    // «Заплановано»
    InProgress, // «Виконується»
    Done        // «Виконано»
}

public enum PartStatus
{
    Awaiting, // «Очікується»
    Received  // «Отримано»
}

public enum WorkCategory
{
    BodyKit,    // обвіс
    Suspension, // підвіска
    Exhaust,    // вихлоп
    Lighting,   // освітлення
    ChipTuning, // чип-тюнінг
    Detailing,  // детейлінг
    Other       // інше
}

public enum Role
{
    Manager,  // Менеджер
    Master,   // Майстер
    Director  // Керівник
}

public static class OrderStatuses
{
    public static readonly OrderStatus[] Current = [OrderStatus.New, OrderStatus.InProgress, OrderStatus.Ready];
    public static readonly OrderStatus[] Completed = [OrderStatus.Issued, OrderStatus.Cancelled];

    public static bool IsActive(OrderStatus status) => Current.Contains(status);
}
