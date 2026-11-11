using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Application;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;
using TuningShop.Api.Infrastructure;

// Повідомлення службових команд українською мають коректно відображатися в консолі Windows.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Службові команди розгортання: seed-demo | seed-volume <кількість> | backup <файл>.
var command = args.FirstOrDefault(a => a is "seed-demo" or "seed-volume" or "backup");
var commandArgument = command is null ? null : args.SkipWhile(a => a != command).Skip(1).FirstOrDefault();
var hostArgs = args.Where(a => a != command && a != commandArgument).ToArray();

var builder = WebApplication.CreateBuilder(hostArgs);

// ---- Корінь композиції: сховище, служби, автентифікація ----

var connectionString = SqlitePaths.Resolve(
    builder.Configuration.GetConnectionString("Tuning")
        ?? throw new InvalidOperationException("Не задано ConnectionStrings:Tuning."),
    builder.Environment.ContentRootPath);
builder.Services.AddDbContext<TuningDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<CarService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderStatusService>();
builder.Services.AddScoped<OrderQueryService>();
builder.Services.AddScoped<MasterWorkService>();
builder.Services.AddScoped<PartService>();
builder.Services.AddScoped<TrackingService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ErrorHandling.InvalidModelState);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "tuning_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        // NFR-02: сесія завершується після 30 хв бездіяльності.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        // Для програмного інтерфейсу — статуси 401/403 з ApiError замість перенаправлень.
        options.Events.OnRedirectToLogin = context =>
            ErrorHandling.WriteAsync(context.HttpContext, 401, "unauthorized", "Потрібно увійти в систему.");
        options.Events.OnRedirectToAccessDenied = context =>
            ErrorHandling.WriteAsync(context.HttpContext, 403, "forbidden", "Ця функція недоступна для вашої ролі.");
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---- Підготовка бази та службові команди ----

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TuningDbContext>();
    await DbInitializer.InitializeAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<Employee>>());

    switch (command)
    {
        case "seed-demo":
            await DemoData.SeedDemoAsync(db, DateTime.UtcNow);
            Console.WriteLine("Демонстраційні дані додано.");
            return;
        case "seed-volume":
            var count = int.Parse(commandArgument ?? "5000");
            await DemoData.SeedVolumeAsync(db, count, DateTime.UtcNow);
            Console.WriteLine($"Додано {count} замовлень для перевірки NFR-01.");
            return;
        case "backup":
            var target = Path.GetFullPath(commandArgument ?? throw new ArgumentException("Вкажіть файл резервної копії."));
            await using (var source = new SqliteConnection(connectionString))
            await using (var destination = new SqliteConnection($"Data Source={target}"))
            {
                await source.OpenAsync();
                await destination.OpenAsync();
                source.BackupDatabase(destination);
            }
            Console.WriteLine($"Резервну копію збережено: {target}");
            return;
    }
}

// ---- Конвеєр обробки запитів ----

app.UseApiErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Зібраний клієнт (статичний експорт Next.js) розміщується у wwwroot пакета випуску.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Map("/api/{**path}", (HttpContext context) =>
    ErrorHandling.WriteAsync(context, 404, "not_found", "Маршрут не знайдено."));

app.Run();

public partial class Program;
