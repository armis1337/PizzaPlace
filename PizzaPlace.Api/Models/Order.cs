using PizzaPlace.Api.Models.Enums;

namespace PizzaPlace.Api.Models;

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public string? ClaimedByDeliveryUser { get; set; }
    public string? CancellationReason { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
