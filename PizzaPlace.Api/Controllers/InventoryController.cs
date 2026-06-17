using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/inventory")]
[Authorize(Roles = nameof(UserRole.Supervisor))]
public class InventoryController(InventoryService inventory) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetInventory() =>
        Ok(await inventory.GetInventoryAsync());

    [HttpPost("{id}/restock")]
    public async Task<IActionResult> Restock(int id) =>
        ToResult(await inventory.RestockAsync(id));
}
