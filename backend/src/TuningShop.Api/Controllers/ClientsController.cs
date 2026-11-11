using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Infrastructure;

namespace TuningShop.Api.Controllers;

/// <summary>UC-01, UC-02, UC-08 (FR-02 – FR-04, FR-18).</summary>
[Route("api/clients")]
[Authorize(Roles = Roles.ManagerOrDirector)]
public sealed class ClientsController(ClientService clients, CarService cars) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? query) => Ok(await clients.SearchAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Respond(await clients.GetAsync(id));

    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Create(SaveClientRequest request) =>
        Respond(await clients.CreateAsync(request), StatusCodes.Status201Created);

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Update(int id, SaveClientRequest request) =>
        Respond(await clients.UpdateAsync(id, request));

    [HttpPost("{id:int}/cars")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> AddCar(int id, SaveCarRequest request) =>
        Respond(await cars.CreateAsync(id, request), StatusCodes.Status201Created);
}
