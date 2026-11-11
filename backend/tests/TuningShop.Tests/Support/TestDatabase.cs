using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Tests.Support;

/// <summary>
/// Справжня база SQLite в пам'яті зі схемою, відтвореною міграціями. З'єднання відкрите,
/// доки живе об'єкт, тому кожен новий контекст бачить ті самі дані — як після повторного читання.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    public static readonly DateTime Now = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TuningDbContext> _options;

    private TestDatabase(SqliteConnection connection)
    {
        _connection = connection;
        _options = new DbContextOptionsBuilder<TuningDbContext>().UseSqlite(connection).Options;
    }

    public FixedClock Clock { get; } = new(Now);

    /// <summary>Лічильник для унікальних телефонів і VIN у <see cref="Builders.NewOrderAsync"/>.</summary>
    public int OrdersCreated;
    public Employee Manager { get; private set; } = null!;
    public Employee Master1 { get; private set; } = null!;
    public Employee Master2 { get; private set; } = null!;
    public Employee Director { get; private set; } = null!;

    public static async Task<TestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var db = new TestDatabase(connection);
        await using var context = db.NewContext();
        await context.Database.MigrateAsync();

        db.Manager = new Employee("Менеджер Тест", "manager", "hash", Role.Manager);
        db.Master1 = new Employee("Майстер Перший", "master1", "hash", Role.Master);
        db.Master2 = new Employee("Майстер Другий", "master2", "hash", Role.Master);
        db.Director = new Employee("Керівник Тест", "director", "hash", Role.Director);
        context.Employees.AddRange(db.Manager, db.Master1, db.Master2, db.Director);
        await context.SaveChangesAsync();
        return db;
    }

    public TuningDbContext NewContext() => new(_options);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}

public sealed class FixedClock(DateTime now) : TimeProvider
{
    public DateTime UtcNow { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}
