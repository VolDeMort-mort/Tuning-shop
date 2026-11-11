using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Tests.Api;

/// <summary>
/// Інтеграційні перевірки через програмний інтерфейс: наскрізний сценарій ЛР №1,
/// некоректні введення (AC-22) і збереження даних після перезапуску сервера.
/// </summary>
public sealed class EndToEndTests : IAsyncLifetime
{
    private readonly string _databasePath = ApiFactory.NewDatabasePath();
    private ApiFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new ApiFactory(_databasePath);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        ApiFactory.DeleteDatabase(_databasePath);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
    }

    [Fact]
    public async Task FullScenario_FromRegistrationToIssue_ThenReadAfterRestart()
    {
        var manager = await _factory.LoginAsync("manager", "manager123");
        var masters = await Read<List<EmployeeDto>>(await manager.GetAsync("/api/employees/masters"), HttpStatusCode.OK);
        var master1 = masters.First(m => m.FullName == "Петренко Олег");

        // UC-01, UC-02
        var client = await Read<ClientDto>(await manager.PostAsJsonAsync("/api/clients",
            new { fullName = "Гончар Василь", phone = "+380631234567" }), HttpStatusCode.Created);
        var car = await Read<CarDto>(await manager.PostAsJsonAsync($"/api/clients/{client.Id}/cars",
            new { make = "Mazda", model = "MX-5", year = 2020, vin = "JM1NDAD75L0412345", plateNumber = "AE5555KI" }), HttpStatusCode.Created);

        // UC-11 → UC-03
        Assert.Single(await Read<List<CarDto>>(await manager.GetAsync("/api/cars?query=AE5555KI"), HttpStatusCode.OK));
        var order = await Read<OrderDetailsDto>(await manager.PostAsJsonAsync("/api/orders", new
        {
            carId = car.Id,
            clientRequest = "Спортивний вигляд",
            works = new object[]
            {
                new { category = "Exhaust", description = "Спортивний вихлоп", estimatedCost = 18000, masterId = master1.Id },
                new { category = "Lighting", description = "LED-фари", estimatedCost = 9000.5 }
            }
        }), HttpStatusCode.Created);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Equal(27000.5m, order.TotalEstimate);

        // A4 → FR-09: без майстра в роботу не передати; після призначення — можна.
        var refused = await Read<ApiError>(await manager.PostAsync($"/api/orders/{order.Id}/start-work", null), HttpStatusCode.Conflict);
        Assert.Equal("masters_not_assigned", refused.Code);
        await Read<OrderDetailsDto>(await manager.PutAsJsonAsync($"/api/works/{order.Works[1].Id}/master",
            new { masterId = master1.Id }), HttpStatusCode.OK);
        await Read<OrderDetailsDto>(await manager.PostAsJsonAsync($"/api/works/{order.Works[1].Id}/parts",
            new { name = "Фари LED", quantity = 2 }), HttpStatusCode.Created);
        await Read<OrderDetailsDto>(await manager.PostAsync($"/api/orders/{order.Id}/start-work", null), HttpStatusCode.OK);

        // UC-05
        var master = await _factory.LoginAsync("master1", "master123");
        var works = await Read<List<MasterWorkDto>>(await master.GetAsync("/api/master/works"), HttpStatusCode.OK);
        Assert.Equal(2, works.Count);
        foreach (var w in works)
        {
            await Read<MasterWorkDto>(await master.PostAsync($"/api/master/works/{w.WorkId}/advance", null), HttpStatusCode.OK);
            await Read<MasterWorkDto>(await master.PostAsync($"/api/master/works/{w.WorkId}/advance", null), HttpStatusCode.OK);
        }

        // UC-10: клієнт бачить прогрес без входу.
        var anonymous = _factory.CreateClient();
        var tracking = await Read<TrackingDto>(await anonymous.GetAsync(
            $"/api/tracking?number={order.Number}&phone=%2B380631234567"), HttpStatusCode.OK);
        Assert.Equal("виконано 2 з 2", tracking.Progress);

        // UC-04
        await Read<OrderDetailsDto>(await manager.PostAsync($"/api/orders/{order.Id}/mark-ready", null), HttpStatusCode.OK);
        var issued = await Read<OrderDetailsDto>(await manager.PostAsync($"/api/orders/{order.Id}/issue", null), HttpStatusCode.OK);
        Assert.Equal(OrderStatus.Issued, issued.Status);
        Assert.Equal(4, issued.History.Count);

        // Перезапуск: нова фабрика з тим самим файлом бази читає ті самі дані.
        await _factory.DisposeAsync();
        _factory = new ApiFactory(_databasePath);
        var afterRestart = await _factory.LoginAsync("manager", "manager123");
        var stored = await Read<OrderDetailsDto>(await afterRestart.GetAsync($"/api/orders/{order.Id}"), HttpStatusCode.OK);
        Assert.Equal(OrderStatus.Issued, stored.Status);
        Assert.Equal(issued.IssuedAt, stored.IssuedAt);
        Assert.Equal("Фари LED", stored.Works[1].Parts.Single().Name);
        Assert.EndsWith("Z", (await (await afterRestart.GetAsync($"/api/orders/{order.Id}")).Content.ReadAsStringAsync())
            .Split("\"createdAt\":\"")[1].Split('"')[0]);
    }

    /// <summary>AC-22: набір із 10 некоректних введень не змінює БД, для кожного показано повідомлення.</summary>
    [Fact]
    public async Task AC22_TenInvalidInputs_DoNotChangeDatabase()
    {
        var manager = await _factory.LoginAsync("manager", "manager123");
        var client = await Read<ClientDto>(await manager.PostAsJsonAsync("/api/clients",
            new { fullName = "Існуючий", phone = "+380631234567" }), HttpStatusCode.Created);
        var car = await Read<CarDto>(await manager.PostAsJsonAsync($"/api/clients/{client.Id}/cars",
            new { make = "Mazda", model = "MX-5", year = 2020, vin = "JM1NDAD75L0412345", plateNumber = "AE5555KI" }), HttpStatusCode.Created);

        var (clients, cars, orders) = await CountsAsync();

        var invalid = new (string Url, object Body, string Field)[]
        {
            ("/api/clients", new { fullName = "", phone = "+380631111111" }, "fullName"),
            ("/api/clients", new { fullName = "Без телефону" }, "phone"),
            ("/api/clients", new { fullName = "Короткий", phone = "+38063111" }, "phone"),
            ("/api/clients", new { fullName = "Не той формат", phone = "0631111111" }, "phone"),
            ("/api/clients", new { fullName = "Пошта", phone = "+380631111112", email = "bad@" }, "email"),
            ($"/api/clients/{client.Id}/cars", new { make = "BMW", model = "M3", year = 2021, vin = "WBS8M9C50M5K1234", plateNumber = "AA1111AA" }, "vin"),
            ($"/api/clients/{client.Id}/cars", new { make = "BMW", model = "M3", year = 2021, vin = "WBS8M9C50M5KI2345", plateNumber = "AA1111AA" }, "vin"),
            ($"/api/clients/{client.Id}/cars", new { make = "BMW", model = "M3", year = 1800, vin = "WBS8M9C50M5K12345", plateNumber = "AA1111AA" }, "year"),
            ("/api/orders", new { carId = car.Id, works = new[] { new { category = "Exhaust", description = "Вихлоп", estimatedCost = -1 } } }, "works[0].estimatedCost"),
            ("/api/orders", new { carId = car.Id, works = new[] { new { category = "Exhaust", description = "", estimatedCost = 10 } } }, "works[0].description"),
        };

        foreach (var (url, body, field) in invalid)
        {
            var error = await Read<ApiError>(await manager.PostAsJsonAsync(url, body), HttpStatusCode.BadRequest);
            Assert.True(error.Fields?.ContainsKey(field) == true, $"{url}: очікувалось поле {field}");
            Assert.False(string.IsNullOrWhiteSpace(error.Fields![field]));
        }

        Assert.Equal((clients, cars, orders), await CountsAsync());
    }

    [Fact]
    public async Task MalformedJson_AndUnknownEnum_Return400_WithApiError()
    {
        var manager = await _factory.LoginAsync("manager", "manager123");
        var malformed = await manager.PostAsync("/api/clients",
            new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal("invalid_request", (await Read<ApiError>(malformed, HttpStatusCode.BadRequest)).Code);

        var badEnum = await manager.PostAsJsonAsync("/api/orders",
            new { carId = 1, works = new[] { new { category = "Rocket", description = "x", estimatedCost = 1 } } });
        var error = await Read<ApiError>(badEnum, HttpStatusCode.BadRequest);
        Assert.Contains("works[0].category", error.Fields!.Keys);

        Assert.Equal("not_found", (await Read<ApiError>(await manager.GetAsync("/api/unknown"), HttpStatusCode.NotFound)).Code);
    }

    private async Task<(int, int, int)> CountsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TuningDbContext>();
        return (await db.Clients.CountAsync(), await db.Cars.CountAsync(), await db.Orders.CountAsync());
    }
}
