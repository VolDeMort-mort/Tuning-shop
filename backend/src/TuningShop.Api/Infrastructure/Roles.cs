using TuningShop.Api.Domain;

namespace TuningShop.Api.Infrastructure;

/// <summary>Назви ролей для атрибутів [Authorize(Roles = ...)] (FR-01).</summary>
public static class Roles
{
    public const string Manager = nameof(Role.Manager);
    public const string Master = nameof(Role.Master);
    public const string Director = nameof(Role.Director);
    public const string ManagerOrDirector = Manager + "," + Director;
}
