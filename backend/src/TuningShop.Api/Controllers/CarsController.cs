using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Infrastructure;

namespace TuningShop.Api.Controllers;

/// <summary>UC-02, UC-11 (FR-05, FR-18).</summary>
[Route("api/cars")]
[Authorize(Roles = Roles.ManagerOrDirector)]
public sealed class CarsController(CarService cars) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? query) => Ok(await cars.SearchAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Respond(await cars.GetAsync(id));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Update(int id, SaveCarRequest request) => Respond(await cars.UpdateAsync(id, request));
}
