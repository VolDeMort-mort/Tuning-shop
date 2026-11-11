using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-02 «Додати автомобіль клієнта», UC-11 «Переглянути автомобіль» (FR-04, FR-05, FR-18).</summary>
public sealed class CarService(TuningDbContext db, TimeProvider clock)
{
    /// <summary>Пошук за повним держномером або VIN (AC-18).</summary>
    public async Task<IReadOnlyList<CarDto>> SearchAsync(string? query)
    {
        var plate = Validation.NormalizePlate(query);
        if (plate.Length == 0) return [];
        var vin = Validation.NormalizeVin(query);

        var found = await db.Cars.AsNoTracking()
            .Where(c => c.PlateNumber == plate || c.Vin == vin)
            .OrderBy(c => c.Id).ToListAsync();
        return found.Select(c => c.ToDto()).ToList();
    }

    public async Task<Result<CarDetailsDto>> GetAsync(int id)
    {
        var car = await db.Cars.AsNoTracking().Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == id);
        if (car is null) return CarNotFound();

        var orders = await db.Orders.AsNoTracking().WithSummary()
            .Where(o => o.CarId == id)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        var active = orders.FirstOrDefault(o => OrderStatuses.IsActive(o.Status));

        return new CarDetailsDto(car.ToDto(), car.Client!.ToDto(), active?.Id, active?.Number,
            orders.Select(o => o.ToSummary()).ToList());
    }

    public async Task<Result<CarDto>> CreateAsync(int clientId, SaveCarRequest request)
    {
        if (!await db.Clients.AnyAsync(c => c.Id == clientId))
            return DomainError.NotFound("client_not_found", "Клієнта не знайдено.");

        var (vin, plate, error) = await ValidateAsync(request, null);
        if (error is not null) return error;

        var car = new Car(clientId, request.Make!, request.Model!, request.Year!.Value, vin, plate);
        db.Cars.Add(car);
        await db.SaveChangesAsync();
        return car.ToDto();
    }

    public async Task<Result<CarDto>> UpdateAsync(int id, SaveCarRequest request)
    {
        var car = await db.Cars.FindAsync(id);
        if (car is null) return CarNotFound();

        var (vin, plate, error) = await ValidateAsync(request, id);
        if (error is not null) return error;

        car.Update(request.Make!, request.Model!, request.Year!.Value, vin, plate);
        await db.SaveChangesAsync();
        return car.ToDto();
    }

    private async Task<(string Vin, string Plate, DomainError? Error)> ValidateAsync(SaveCarRequest request, int? currentId)
    {
        var vin = Validation.NormalizeVin(request.Vin);
        var plate = Validation.NormalizePlate(request.PlateNumber);
        var errors = Validation.ValidateCar(request.Make, request.Model, request.Year, vin, plate,
            clock.GetUtcNow().Year);
        if (errors.Count > 0) return (vin, plate, DomainError.Invalid(errors));

        if (await db.Cars.AnyAsync(c => c.Vin == vin && c.Id != currentId))
            return (vin, plate, new DomainError("vin_exists", "Автомобіль із таким VIN уже зареєстрований.",
                Fields: new Dictionary<string, string> { ["vin"] = "Цей VIN уже є в системі." }));
        return (vin, plate, null);
    }

    private static DomainError CarNotFound() => DomainError.NotFound("car_not_found", "Автомобіль не знайдено.");
}
