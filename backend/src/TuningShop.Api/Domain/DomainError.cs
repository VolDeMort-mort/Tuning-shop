namespace TuningShop.Api.Domain;

/// <summary>Вид відмови визначає HTTP-статус, який обробник маршруту поверне клієнту.</summary>
public enum ErrorKind
{
    Validation, // 400 — некоректні дані запиту (NFR-03)
    NotFound,   // 404
    Conflict,   // 409 — порушення предметного правила
    Forbidden   // 403
}

/// <summary>
/// Очікувана предметна відмова. Не є винятком: служба повертає її як результат операції,
/// а дані при цьому не змінюються.
/// </summary>
public sealed record DomainError(
    string Code,
    string Message,
    ErrorKind Kind = ErrorKind.Conflict,
    IReadOnlyDictionary<string, string>? Fields = null,
    IReadOnlyList<string>? Details = null)
{
    public static DomainError NotFound(string code, string message) => new(code, message, ErrorKind.NotFound);

    public static DomainError Invalid(IReadOnlyDictionary<string, string> fields) =>
        new("validation_failed", "Дані містять помилки. Виправте позначені поля.", ErrorKind.Validation, fields);

    public static DomainError Invalid(string field, string message) =>
        Invalid(new Dictionary<string, string> { [field] = message });
}
