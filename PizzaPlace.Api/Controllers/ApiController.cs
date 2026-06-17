using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Common;

namespace PizzaPlace.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult ToResult<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);

        return result.ErrorStatusCode switch
        {
            400 when result.ErrorDetail is not null =>
                BadRequest(new { message = result.ErrorMessage, deficit = result.ErrorDetail }),
            400 => BadRequest(new { message = result.ErrorMessage }),
            401 => Unauthorized(new { message = result.ErrorMessage }),
            403 => Forbid(),
            404 => NotFound(),
            _   => StatusCode(result.ErrorStatusCode, new { message = result.ErrorMessage })
        };
    }
}
