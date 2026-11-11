using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;

namespace TuningShop.Tests.Support;

/// <summary>Підготовка даних через ті самі служби, якими користується програма.</summary>
public static class Builders
{
    public static async Task<ClientDto> ClientAsync(this TestDatabase db,
        string fullName = "Бондар Андрій Петрович", string phone = "+380501112233")
    {
        await using var context = db.NewContext();
        var result = await new ClientService(context).CreateAsync(new SaveClientRequest(fullName, phone, null));
        return result.Value ?? throw new InvalidOperationException(result.Error!.Message);
    }

    public static async Task<CarDto> CarAsync(this TestDatabase db, int clientId,
        string vin = "WVWZZZAUZKW123456", string plate = "AA1234BB")
    {
        await using var context = db.NewContext();
        var result = await new CarService(context, db.Clock)
            .CreateAsync(clientId, new SaveCarRequest("Volkswagen", "Golf", 2019, vin, plate));
        return result.Value ?? throw new InvalidOperationException(result.Error!.Message);
    }

    public static WorkRequest Work(int? masterId = null, string description = "Встановлення спліттера",
        decimal cost = 1000m, WorkCategory category = WorkCategory.BodyKit) =>
        new(category, description, cost, masterId);

    public static async Task<Result<OrderDetailsDto>> CreateOrderAsync(this TestDatabase db, int carId,
        params WorkRequest[] works)
    {
        await using var context = db.NewContext();
        return await new OrderService(context, db.Clock)
            .CreateAsync(new CreateOrderRequest(carId, "Побажання клієнта", works), db.Manager.Id);
    }

    /// <summary>Клієнт, автомобіль і замовлення «Нове» з роботами.</summary>
    public static async Task<OrderDetailsDto> NewOrderAsync(this TestDatabase db, params WorkRequest[] works)
    {
        // Перше замовлення тесту — для типового клієнта; наступні — для нових клієнтів з унікальними даними.
        var n = Interlocked.Increment(ref db.OrdersCreated);
        var client = n == 1 ? await db.ClientAsync() : await db.ClientAsync($"Клієнт {n}", $"+38050000{n:D4}");
        var car = n == 1 ? await db.CarAsync(client.Id) : await db.CarAsync(client.Id, $"WVWZZZAUZKW1{n:D5}", $"AA{n:D4}BB");
        var result = await db.CreateOrderAsync(car.Id, works.Length > 0 ? works : [Work(db.Master1.Id)]);
        return result.Value ?? throw new InvalidOperationException(result.Error!.Message);
    }

    public static async Task<OrderDetailsDto> InProgressOrderAsync(this TestDatabase db, params WorkRequest[] works)
    {
        var order = await db.NewOrderAsync(works);
        await using var context = db.NewContext();
        var started = await new OrderStatusService(context, db.Clock).StartWorkAsync(order.Id, db.Manager.Id);
        return started.Value ?? throw new InvalidOperationException(started.Error!.Message);
    }

    public static async Task CompleteAllWorksAsync(this TestDatabase db, OrderDetailsDto order)
    {
        foreach (var work in order.Works)
        {
            await using var context = db.NewContext();
            var service = new MasterWorkService(context, db.Clock);
            await service.AdvanceAsync(work.Id, work.MasterId!.Value);
            await service.AdvanceAsync(work.Id, work.MasterId!.Value);
        }
    }

    public static async Task<OrderDetailsDto> ReloadAsync(this TestDatabase db, int orderId)
    {
        await using var context = db.NewContext();
        return (await new OrderQueryService(context).GetDetailsAsync(orderId)).Value!;
    }
}
