using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;
using TuningShop.Api.Infrastructure;

namespace TuningShop.Api.Controllers;

/// <summary>UC-03, UC-04, UC-06, UC-07 (FR-06, FR-07, FR-09, FR-12 – FR-17, FR-19).</summary>
[Route("api/orders")]
[Authorize(Roles = Roles.ManagerOrDirector)]
public sealed class OrdersController(
    OrderService orders, OrderStatusService statuses, OrderQueryService queries) : ApiControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> Current([FromQuery] OrderStatus? status, [FromQuery] int? masterId) =>
        Respond(await queries.GetCurrentAsync(status, masterId));

    [HttpGet("completed")]
    public async Task<IActionResult> Completed([FromQuery] DateOnly? from, [FromQuery] DateOnly? to) =>
        Respond(await queries.GetCompletedAsync(from, to));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Respond(await queries.GetDetailsAsync(id));

    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Create(CreateOrderRequest request) =>
        Respond(await orders.CreateAsync(request, CurrentEmployeeId), StatusCodes.Status201Created);

    [HttpPost("{id:int}/works")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> AddWork(int id, WorkRequest request) =>
        Respond(await orders.AddWorkAsync(id, request), StatusCodes.Status201Created);

    [HttpPost("{id:int}/start-work")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> StartWork(int id) => Respond(await statuses.StartWorkAsync(id, CurrentEmployeeId));

    [HttpPost("{id:int}/mark-ready")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> MarkReady(int id) => Respond(await statuses.MarkReadyAsync(id, CurrentEmployeeId));

    [HttpPost("{id:int}/issue")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Issue(int id) => Respond(await statuses.IssueAsync(id, CurrentEmployeeId));

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Cancel(int id, CancelOrderRequest request) =>
        Respond(await statuses.CancelAsync(id, request, CurrentEmployeeId));
}
