using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/kitchen")]
[Authorize(Roles = nameof(UserRole.Chef))]
public class KitchenController(KitchenService kitchen) : ApiController
{
    [HttpGet("orders")]
    public async Task<IActionResult> GetKitchenOrders() =>
        Ok(await kitchen.GetKitchenOrdersAsync());

    [HttpPost("orders/{id}/start")]
    public async Task<IActionResult> StartOrder(int id) =>
        ToResult(await kitchen.StartOrderAsync(id));

    [HttpPost("orders/{id}/ready")]
    public async Task<IActionResult> MarkReady(int id) =>
        ToResult(await kitchen.MarkReadyAsync(id));

    [HttpPost("orders/{id}/cancel")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest? req = null) =>
        ToResult(await kitchen.CancelOrderAsync(id, req?.Reason));
}

public record CancelOrderRequest(string? Reason);
