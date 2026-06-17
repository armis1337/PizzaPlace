using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/stats")]
[Authorize(Roles = nameof(UserRole.Supervisor))]
public class StatsController(StatsService stats) : ApiController
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() =>
        Ok(await stats.GetSummaryAsync());
}
