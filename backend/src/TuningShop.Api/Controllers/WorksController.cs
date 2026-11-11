using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Infrastructure;

namespace TuningShop.Api.Controllers;

/// <summary>Роботи й деталі замовлення: FR-07, FR-08, FR-20. Кожна дія повертає оновлену картку замовлення.</summary>
[Route("api")]
[Authorize(Roles = Roles.Manager)]
public sealed class WorksController(OrderService orders, PartService parts) : ApiControllerBase
{
    [HttpGet("employees/masters")]
    public async Task<IActionResult> Masters() => Ok(await orders.GetMastersAsync());

    [HttpPut("works/{workId:int}")]
    public async Task<IActionResult> Update(int workId, WorkRequest request) =>
        Respond(await orders.UpdateWorkAsync(workId, request));

    [HttpDelete("works/{workId:int}")]
    public async Task<IActionResult> Delete(int workId) => Respond(await orders.DeleteWorkAsync(workId));

    [HttpPut("works/{workId:int}/master")]
    public async Task<IActionResult> AssignMaster(int workId, AssignMasterRequest request) =>
        Respond(await orders.AssignMasterAsync(workId, request));

    [HttpPost("works/{workId:int}/parts")]
    public async Task<IActionResult> AddPart(int workId, AddPartRequest request) =>
        Respond(await parts.AddAsync(workId, request), StatusCodes.Status201Created);

    [HttpPost("parts/{partId:int}/receive")]
    public async Task<IActionResult> ReceivePart(int partId) => Respond(await parts.MarkReceivedAsync(partId));
}
