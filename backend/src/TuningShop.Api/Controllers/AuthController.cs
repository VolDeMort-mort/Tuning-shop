using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Controllers;

/// <summary>FR-01: вхід за логіном і паролем, сесія в cookie з тайм-аутом бездіяльності 30 хв (NFR-02).</summary>
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ApiControllerBase
{
    private const string LoginClaim = "login";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request);
        if (!result.IsSuccess) return Respond(result);

        var user = result.Value!;
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(LoginClaim, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Ok(user);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new CurrentUserDto(
        CurrentEmployeeId,
        User.FindFirstValue(ClaimTypes.Name) ?? "",
        User.FindFirstValue(LoginClaim) ?? "",
        Enum.Parse<Role>(User.FindFirstValue(ClaimTypes.Role)!)));
}
