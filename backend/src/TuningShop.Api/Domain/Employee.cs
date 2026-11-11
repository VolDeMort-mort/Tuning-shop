namespace TuningShop.Api.Domain;

/// <summary>Працівник ательє. Облікові записи створюються під час розгортання (SRS, межі v1.0).</summary>
public sealed class Employee
{
    private Employee() { }

    public Employee(string fullName, string login, string passwordHash, Role role)
    {
        FullName = fullName;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    public int Id { get; private set; }
    public string FullName { get; private set; } = "";
    public string Login { get; private set; } = "";

    /// <summary>Лише хеш пароля, відкритий пароль не зберігається (NFR-02).</summary>
    public string PasswordHash { get; private set; } = "";
    public Role Role { get; private set; }
}
