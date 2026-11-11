namespace TuningShop.Api.Domain;

public sealed class Car
{
    private Car() { }

    public Car(int clientId, string make, string model, int year, string vin, string plateNumber)
    {
        ClientId = clientId;
        Update(make, model, year, vin, plateNumber);
    }

    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public Client? Client { get; private set; }
    public string Make { get; private set; } = "";
    public string Model { get; private set; } = "";
    public int Year { get; private set; }
    public string Vin { get; private set; } = "";
    public string PlateNumber { get; private set; } = "";
    public List<Order> Orders { get; private set; } = [];

    /// <summary>Дані вже перевірено <see cref="Validation.ValidateCar"/>.</summary>
    public void Update(string make, string model, int year, string vin, string plateNumber)
    {
        Make = make.Trim();
        Model = model.Trim();
        Year = year;
        Vin = vin;
        PlateNumber = plateNumber;
    }

    /// <summary>Активне — будь-яке замовлення, крім «Видане» і «Скасоване» (FR-06). Потребує завантажених Orders.</summary>
    public Order? ActiveOrder() => Orders.FirstOrDefault(o => OrderStatuses.IsActive(o.Status));

    public bool HasActiveOrder() => ActiveOrder() is not null;
}
