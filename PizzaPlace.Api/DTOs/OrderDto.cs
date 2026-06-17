using PizzaPlace.Api.Models;

namespace PizzaPlace.Api.DTOs;

public record OrderItemDto(int PizzaId, string PizzaName, int Quantity);

public record OrderDto(
    int Id,
    string CustomerName,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal TotalPrice,
    string? ClaimedByDeliveryUser,
    string? CancellationReason,
    IEnumerable<OrderItemDto> Items,
    // Kitchen-only fulfillability info; null on non-kitchen views.
    bool? CanStart = null,
    IEnumerable<string>? BlockingIngredients = null);

public static class OrderMapping
{
    public static OrderDto ToDto(this Order o) => new(
        o.Id,
        o.CustomerName,
        o.Status.ToString(),
        o.CreatedAt,
        o.UpdatedAt,
        o.TotalPrice,
        o.ClaimedByDeliveryUser,
        o.CancellationReason,
        o.Items.Select(i => new OrderItemDto(i.PizzaId, i.Pizza.Name, i.Quantity)));
}
