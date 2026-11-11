using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Infrastructure;

namespace TuningShop.Api.Controllers;

/// <summary>UC-05 «Виконати призначену роботу» (FR-10, FR-11).</summary>
[Route("api/master/works")]
[Authorize(Roles = Roles.Master)]
public sealed class MasterController(MasterWorkService works) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> MyWorks() => Ok(await works.GetMyWorksAsync(CurrentEmployeeId));

    [HttpPost("{workId:int}/advance")]
    public async Task<IActionResult> Advance(int workId) => Respond(await works.AdvanceAsync(workId, CurrentEmployeeId));
}
