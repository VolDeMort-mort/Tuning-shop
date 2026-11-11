using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;
using TuningShop.Api.Data;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Application;

/// <summary>Вхід працівника за логіном і паролем (FR-01, NFR-02).</summary>
public sealed class AuthService(TuningDbContext db, IPasswordHasher<Employee> hasher)
{
    public async Task<Result<CurrentUserDto>> LoginAsync(LoginRequest request)
    {
        var login = (request.Login ?? "").Trim();
        var employee = login.Length == 0
            ? null
            : await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Login == login);

        if (employee is null || string.IsNullOrEmpty(request.Password) ||
            hasher.VerifyHashedPassword(employee, employee.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return new DomainError("invalid_credentials", "Невірний логін або пароль.", ErrorKind.Validation);

        return new CurrentUserDto(employee.Id, employee.FullName, employee.Login, employee.Role);
    }
}
