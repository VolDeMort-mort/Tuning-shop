using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;

namespace TuningShop.Api.Controllers;

/// <summary>UC-10: єдина функція, доступна без входу (FR-01, FR-21).</summary>
[Route("api/tracking")]
[AllowAnonymous]
public sealed class TrackingController(TrackingService tracking) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Track([FromQuery] string? number, [FromQuery] string? phone) =>
        Respond(await tracking.TrackAsync(number, phone));
}
