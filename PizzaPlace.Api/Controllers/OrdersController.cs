using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/orders")]
public class OrdersController(OrderService orders) : ApiController
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest req) =>
        ToResult(await orders.PlaceOrderAsync(
            req.CustomerName,
            req.Items.Select(i => (i.PizzaId, i.Quantity)).ToList()));

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var dto = await orders.GetOrderAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = nameof(UserRole.Supervisor))]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status) =>
        Ok(await orders.GetAllOrdersAsync(status));
}

public record PlaceOrderRequest(string CustomerName, List<OrderItemRequest> Items);
public record OrderItemRequest(int PizzaId, int Quantity);
