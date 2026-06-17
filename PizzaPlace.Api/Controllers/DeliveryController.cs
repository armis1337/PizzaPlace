using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/delivery")]
[Authorize(Roles = nameof(UserRole.Delivery))]
public class DeliveryController(DeliveryService delivery) : ApiController
{
    private string Username => User.FindFirstValue(ClaimTypes.Name)!;

    [HttpGet("orders")]
    public async Task<IActionResult> GetDeliveryOrders() =>
        Ok(await delivery.GetDeliveryOrdersAsync(Username));

    [HttpPost("orders/{id}/claim")]
    public async Task<IActionResult> ClaimOrder(int id) =>
        ToResult(await delivery.ClaimOrderAsync(id, Username));

    [HttpPost("orders/{id}/deliver")]
    public async Task<IActionResult> DeliverOrder(int id) =>
        ToResult(await delivery.DeliverOrderAsync(id, Username));
}
