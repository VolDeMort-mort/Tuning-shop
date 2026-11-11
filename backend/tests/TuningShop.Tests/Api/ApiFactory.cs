using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace TuningShop.Tests.Api;

/// <summary>
/// Повний сервер у процесі тесту зі справжнім файлом SQLite. Повторне створення фабрики з тим самим
/// файлом моделює перезапуск сервера.
/// </summary>
public sealed class ApiFactory(string databasePath) : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public string DatabasePath { get; } = databasePath;

    public static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"tuning-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Tuning", $"Data Source={DatabasePath}");
    }

    public async Task<HttpClient> LoginAsync(string login, string password)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { login, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    public static void DeleteDatabase(string path)
    {
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { path, path + "-wal", path + "-shm" })
            if (File.Exists(file)) File.Delete(file);
    }
}
