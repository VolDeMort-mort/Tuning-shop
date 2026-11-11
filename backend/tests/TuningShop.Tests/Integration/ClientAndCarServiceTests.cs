using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Tests.Support;

namespace TuningShop.Tests.Integration;

/// <summary>UC-01, UC-02, UC-08, UC-11 зі справжньою базою SQLite (AC-02 – AC-05, AC-18, AC-22).</summary>
public sealed class ClientAndCarServiceTests : IAsyncLifetime
{
    private TestDatabase _db = null!;

    public async Task InitializeAsync() => _db = await TestDatabase.CreateAsync();
    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task AC02_ValidClient_IsSaved_AndFoundByPhone()
    {
        await using (var context = _db.NewContext())
        {
            var result = await new ClientService(context)
                .CreateAsync(new SaveClientRequest(" Бондар Андрій ", "+380 50 111 22 33", "a@b.ua"));
            Assert.True(result.IsSuccess);
            Assert.Equal("+380501112233", result.Value!.Phone);
        }

        await using var verify = _db.NewContext();
        var found = Assert.Single(await new ClientService(verify).SearchAsync("+380501112233"));
        Assert.Equal("Бондар Андрій", found.FullName);
        Assert.Equal("a@b.ua", found.Email);
    }

    [Fact]
    public async Task AC02_DuplicatePhoneOrMissingFields_NotSaved()
    {
        await _db.ClientAsync(phone: "+380501112233");
        await using var context = _db.NewContext();
        var service = new ClientService(context);

        var duplicate = await service.CreateAsync(new SaveClientRequest("Інший", "+380501112233", null));
        Assert.Equal("phone_exists", duplicate.Error?.Code);
        Assert.Contains("phone", duplicate.Error!.Fields!.Keys);

        var missing = await service.CreateAsync(new SaveClientRequest("", null, null));
        Assert.Equal("validation_failed", missing.Error?.Code);
        Assert.Equal(["fullName", "phone"], missing.Error!.Fields!.Keys.Order());

        Assert.Equal(1, await _db.NewContext().Clients.CountAsync());
    }

    [Fact]
    public async Task AC03_EditedClient_ShownInCardAndOrders_InvalidEditNotSaved()
    {
        var order = await _db.NewOrderAsync();
        await using (var context = _db.NewContext())
        {
            var service = new ClientService(context);
            Assert.True((await service.UpdateAsync(order.Client.Id,
                new SaveClientRequest("Бондар-Нове Ім'я", "+380509998877", null))).IsSuccess);
            Assert.Equal("validation_failed",
                (await service.UpdateAsync(order.Client.Id, new SaveClientRequest("X", "123", null))).Error?.Code);
        }

        await using var verify = _db.NewContext();
        var card = (await new ClientService(verify).GetAsync(order.Client.Id)).Value!;
        Assert.Equal("+380509998877", card.Phone);
        Assert.Equal("Бондар-Нове Ім'я", Assert.Single(card.Orders).ClientName);
        Assert.Equal("+380509998877", (await new OrderQueryService(verify).GetDetailsAsync(order.Id)).Value!.Client.Phone);
    }

    [Fact]
    public async Task AC04_ValidCar_LinkedToClient_InvalidOrDuplicateVinRejected()
    {
        var client = await _db.ClientAsync();
        await using var context = _db.NewContext();
        var service = new CarService(context, _db.Clock);

        var ok = await service.CreateAsync(client.Id, new SaveCarRequest("BMW", "M3", 2021, "wbs8m9c50m5k12345", "ka 7777 ax"));
        Assert.True(ok.IsSuccess);
        Assert.Equal("WBS8M9C50M5K12345", ok.Value!.Vin);
        Assert.Equal("KA7777AX", ok.Value.PlateNumber);

        var shortVin = await service.CreateAsync(client.Id, new SaveCarRequest("BMW", "M3", 2021, "WBS8M9C50M5K1234", "AA0000AA"));
        Assert.Equal("vin", Assert.Single(shortVin.Error!.Fields!).Key);

        var duplicate = await service.CreateAsync(client.Id, new SaveCarRequest("Audi", "A4", 2020, "WBS8M9C50M5K12345", "AA0000AA"));
        Assert.Equal("vin_exists", duplicate.Error?.Code);

        var noClient = await service.CreateAsync(999, new SaveCarRequest("Audi", "A4", 2020, "WAUZZZF41HA123456", "AA0000AA"));
        Assert.Equal("client_not_found", noClient.Error?.Code);

        var card = (await new ClientService(_db.NewContext()).GetAsync(client.Id)).Value!;
        Assert.Equal("WBS8M9C50M5K12345", Assert.Single(card.Cars).Vin);
    }

    [Fact]
    public async Task AC05_EditedCar_ShownInCardAndOrders()
    {
        var order = await _db.NewOrderAsync();
        await using (var context = _db.NewContext())
        {
            var service = new CarService(context, _db.Clock);
            var updated = await service.UpdateAsync(order.Car.Id, new SaveCarRequest("Volkswagen", "Golf R", 2020, order.Car.Vin, "AA9999BB"));
            Assert.True(updated.IsSuccess);
            var invalid = await service.UpdateAsync(order.Car.Id, new SaveCarRequest("Volkswagen", "Golf R", 2020, "BAD", "AA9999BB"));
            Assert.False(invalid.IsSuccess);
        }

        await using var verify = _db.NewContext();
        var car = (await new CarService(verify, _db.Clock).GetAsync(order.Car.Id)).Value!;
        Assert.Equal("Golf R", car.Car.Model);
        Assert.Equal(order.Car.Vin, car.Car.Vin);
        Assert.Equal("AA9999BB", Assert.Single(car.Orders).PlateNumber);
    }

    [Fact]
    public async Task AC18_Search_ByPhonePlateVinAndNamePart()
    {
        var a = await _db.ClientAsync("Бондар Андрій Петрович", "+380501112233");
        await _db.ClientAsync("Бондаренко Ірина", "+380671112233");
        await _db.ClientAsync("Мельник Тарас", "+380931112233");
        await _db.CarAsync(a.Id, "WVWZZZAUZKW123456", "AA1234BB");

        await using var context = _db.NewContext();
        var clients = new ClientService(context);
        var cars = new CarService(context, _db.Clock);

        Assert.Equal(a.Id, Assert.Single(await clients.SearchAsync("+380501112233")).Id);
        Assert.Equal(2, (await clients.SearchAsync("бондар")).Count); // частина ПІБ, без урахування регістру
        Assert.Empty(await clients.SearchAsync("Шевченко")); // «Нічого не знайдено»
        Assert.Single(await cars.SearchAsync("aa 1234 bb"));
        Assert.Single(await cars.SearchAsync("WVWZZZAUZKW123456"));
        Assert.Empty(await cars.SearchAsync("ZZ0000ZZ"));
    }
}
