namespace TuningShop.Api.Domain;

/// <summary>Назви значень переліків українською — збігаються з термінами SRS.</summary>
public static class StatusNames
{
    public static string Of(OrderStatus status) => status switch
    {
        OrderStatus.New => "Нове",
        OrderStatus.InProgress => "В роботі",
        OrderStatus.Ready => "Готове до видачі",
        OrderStatus.Issued => "Видане",
        OrderStatus.Cancelled => "Скасоване",
        _ => status.ToString()
    };

    public static string Of(WorkStatus status) => status switch
    {
        WorkStatus.Planned => "Заплановано",
        WorkStatus.InProgress => "Виконується",
        WorkStatus.Done => "Виконано",
        _ => status.ToString()
    };

    public static string Of(PartStatus status) => status switch
    {
        PartStatus.Awaiting => "Очікується",
        PartStatus.Received => "Отримано",
        _ => status.ToString()
    };
}
