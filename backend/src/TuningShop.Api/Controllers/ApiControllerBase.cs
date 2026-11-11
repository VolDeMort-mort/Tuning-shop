using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TuningShop.Api.Application;
using TuningShop.Api.Contracts;
using TuningShop.Api.Domain;

namespace TuningShop.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Ідентифікатор працівника з файла cookie сесії.</summary>
    protected int CurrentEmployeeId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Перетворює результат служби на погоджену відповідь: статус і тіло.</summary>
    protected IActionResult Respond<T>(Result<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
            return StatusCode(successStatus, result.Value);
        return Error(result.Error!);
    }

    protected IActionResult Error(DomainError error)
    {
        var status = error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status409Conflict
        };
        return StatusCode(status, new ApiError(error.Code, error.Message, error.Fields, error.Details));
    }
}
