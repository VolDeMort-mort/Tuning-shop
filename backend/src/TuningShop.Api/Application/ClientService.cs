using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>UC-01 «Зареєструвати клієнта», UC-08 «Переглянути клієнта» (FR-02, FR-03, FR-18).</summary>
public sealed class ClientService(TuningDbContext db)
{
    private const int SearchLimit = 50;

    /// <summary>Пошук за повним телефоном або частиною ПІБ (AC-18).</summary>
    public async Task<IReadOnlyList<ClientDto>> SearchAsync(string? query)
    {
        var text = (query ?? "").Trim();
        if (text.Length == 0) return [];

        var phone = Validation.NormalizePhone(text);
        var namePart = text.ToLowerInvariant();
        IQueryable<Client> clients = Validation.IsValidPhone(phone)
            ? db.Clients.Where(c => c.Phone == phone)
            : db.Clients.Where(c => c.FullNameSearch.Contains(namePart));

        var found = await clients.AsNoTracking().OrderBy(c => c.FullName).Take(SearchLimit).ToListAsync();
        return found.Select(c => c.ToDto()).ToList();
    }

    public async Task<Result<ClientDetailsDto>> GetAsync(int id)
    {
        var client = await db.Clients.AsNoTracking().Include(c => c.Cars).FirstOrDefaultAsync(c => c.Id == id);
        if (client is null) return ClientNotFound();

        var orders = await db.Orders.AsNoTracking().WithSummary()
            .Where(o => o.Car!.ClientId == id)
            .OrderByDescending(o => o.CreatedAt).ToListAsync();

        return new ClientDetailsDto(client.Id, client.FullName, client.Phone, client.Email,
            client.Cars.OrderBy(c => c.Id).Select(c => c.ToDto()).ToList(),
            orders.Select(o => o.ToSummary()).ToList());
    }

    public async Task<Result<ClientDto>> CreateAsync(SaveClientRequest request)
    {
        var (phone, error) = await ValidateAsync(request, null);
        if (error is not null) return error;

        var client = new Client(request.FullName!, phone, request.Email);
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client.ToDto();
    }

    public async Task<Result<ClientDto>> UpdateAsync(int id, SaveClientRequest request)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is null) return ClientNotFound();

        var (phone, error) = await ValidateAsync(request, id);
        if (error is not null) return error;

        client.UpdateContacts(request.FullName!, phone, request.Email);
        await db.SaveChangesAsync();
        return client.ToDto();
    }

    private async Task<(string Phone, DomainError? Error)> ValidateAsync(SaveClientRequest request, int? currentId)
    {
        var phone = Validation.NormalizePhone(request.Phone);
        var errors = Validation.ValidateClient(request.FullName, phone, request.Email);
        if (errors.Count > 0) return (phone, DomainError.Invalid(errors));

        if (await db.Clients.AnyAsync(c => c.Phone == phone && c.Id != currentId))
            return (phone, new DomainError("phone_exists", "Клієнт із таким телефоном уже зареєстрований.",
                Fields: new Dictionary<string, string> { ["phone"] = "Цей телефон уже є в системі." }));
        return (phone, null);
    }

    private static DomainError ClientNotFound() => DomainError.NotFound("client_not_found", "Клієнта не знайдено.");
}
