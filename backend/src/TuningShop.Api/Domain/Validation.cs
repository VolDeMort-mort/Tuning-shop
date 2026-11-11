using System.Text.RegularExpressions;

namespace TuningShop.Api.Domain;

/// <summary>
/// Серверні перевірки форматів і обов'язкових полів (NFR-03). Правила не залежать
/// від HTTP чи бази даних, тому однаково діють для будь-якого клієнта.
/// </summary>
public static partial class Validation
{
    public const int MinCarYear = 1900;

    [GeneratedRegex(@"^\+380\d{9}$")]
    private static partial Regex PhoneRegex();

    // 17 символів; латинські літери без I, O, Q та цифри.
    [GeneratedRegex(@"^[A-HJ-NPR-Z0-9]{17}$")]
    private static partial Regex VinRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public static string NormalizePhone(string? phone) =>
        (phone ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

    public static string NormalizeVin(string? vin) => (vin ?? "").Trim().ToUpperInvariant();

    public static string NormalizePlate(string? plate) => (plate ?? "").Replace(" ", "").ToUpperInvariant();

    public static bool IsValidPhone(string phone) => PhoneRegex().IsMatch(phone);

    public static bool IsValidVin(string vin) => VinRegex().IsMatch(vin);

    public static Dictionary<string, string> ValidateClient(string? fullName, string phone, string? email)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(fullName))
            errors["fullName"] = "Вкажіть ПІБ клієнта.";
        else if (fullName.Trim().Length > 150)
            errors["fullName"] = "ПІБ не може бути довшим за 150 символів.";
        if (string.IsNullOrEmpty(phone))
            errors["phone"] = "Вкажіть номер телефону.";
        else if (!IsValidPhone(phone))
            errors["phone"] = "Телефон має формат +380XXXXXXXXX.";
        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex().IsMatch(email.Trim()))
            errors["email"] = "Некоректна адреса e-mail.";
        return errors;
    }

    public static Dictionary<string, string> ValidateCar(
        string? make, string? model, int? year, string vin, string plate, int currentYear)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(make)) errors["make"] = "Вкажіть марку.";
        if (string.IsNullOrWhiteSpace(model)) errors["model"] = "Вкажіть модель.";
        if (year is null || year < MinCarYear || year > currentYear + 1)
            errors["year"] = $"Рік випуску має бути від {MinCarYear} до {currentYear + 1}.";
        if (vin.Length != 17)
            errors["vin"] = "VIN має містити рівно 17 символів.";
        else if (!IsValidVin(vin))
            errors["vin"] = "VIN містить лише латинські літери (без I, O, Q) і цифри.";
        if (string.IsNullOrEmpty(plate)) errors["plateNumber"] = "Вкажіть державний номер.";
        else if (plate.Length > 12) errors["plateNumber"] = "Державний номер не довший за 12 символів.";
        return errors;
    }

    public static Dictionary<string, string> ValidateWork(WorkCategory? category, string? description, decimal? cost)
    {
        var errors = new Dictionary<string, string>();
        if (category is null || !Enum.IsDefined(category.Value)) errors["category"] = "Оберіть вид роботи.";
        if (string.IsNullOrWhiteSpace(description)) errors["description"] = "Вкажіть опис роботи.";
        if (cost is null || cost < 0) errors["estimatedCost"] = "Орієнтовна вартість має бути не менше 0.";
        else if (decimal.Round(cost.Value, 2) != cost.Value) errors["estimatedCost"] = "Вартість — не більше двох знаків після коми.";
        return errors;
    }

    public static Dictionary<string, string> ValidatePart(string? name, int? quantity)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = "Вкажіть назву деталі.";
        if (quantity is null || quantity < 1) errors["quantity"] = "Кількість має бути не менше 1.";
        return errors;
    }
}
