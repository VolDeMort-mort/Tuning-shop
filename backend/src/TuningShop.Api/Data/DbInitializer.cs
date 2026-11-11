using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Data;

/// <summary>
/// Підготовка бази під час розгортання: застосування міграцій і створення облікових записів
/// працівників (інтерфейсу адміністрування у v1.0 немає — SRS, межі v1.0).
/// </summary>
public static class DbInitializer
{
    public static readonly (string FullName, string Login, string Password, Role Role)[] DefaultEmployees =
    [
        ("Іваненко Марія", "manager", "manager123", Role.Manager),
        ("Петренко Олег", "master1", "master123", Role.Master),
        ("Коваль Ігор", "master2", "master123", Role.Master),
        ("Шевченко Наталія", "director", "director123", Role.Director)
    ];

    public static async Task InitializeAsync(TuningDbContext db, IPasswordHasher<Employee> hasher)
    {
        await db.Database.MigrateAsync();
        if (await db.Employees.AnyAsync()) return;

        foreach (var (fullName, login, password, role) in DefaultEmployees)
        {
            var employee = new Employee(fullName, login, "", role);
            db.Employees.Add(new Employee(fullName, login, hasher.HashPassword(employee, password), role));
        }
        await db.SaveChangesAsync();
    }
}
