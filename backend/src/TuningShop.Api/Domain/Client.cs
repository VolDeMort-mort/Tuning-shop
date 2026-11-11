namespace TuningShop.Api.Domain;

public sealed class Client
{
    private Client() { }

    public Client(string fullName, string phone, string? email)
    {
        UpdateContacts(fullName, phone, email);
    }

    public int Id { get; private set; }
    public string FullName { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public string? Email { get; private set; }
    public List<Car> Cars { get; private set; } = [];

    /// <summary>
    /// ПІБ у нижньому регістрі для пошуку за частиною: LIKE у SQLite не враховує
    /// регістр лише для латиниці, а ПІБ записані кирилицею (FR-18).
    /// </summary>
    public string FullNameSearch { get; private set; } = "";

    /// <summary>Дані вже перевірено <see cref="Validation.ValidateClient"/>.</summary>
    public void UpdateContacts(string fullName, string phone, string? email)
    {
        FullName = fullName.Trim();
        FullNameSearch = FullName.ToLowerInvariant();
        Phone = phone;
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
