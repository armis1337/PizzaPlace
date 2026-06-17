using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Common;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.DTOs;
using PizzaPlace.Api.Hubs;
using PizzaPlace.Api.Models;
using PizzaPlace.Api.Models.Enums;

namespace PizzaPlace.Api.Services;

public class OrderService(AppDbContext db, IHubContext<OrderHub> hub)
{
    public async Task<Result<OrderDto>> PlaceOrderAsync(
        string customerName, IReadOnlyList<(int PizzaId, int Quantity)> items)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Result<OrderDto>.BadRequest("Customer name is required.");
        if (items is null || items.Count == 0)
            return Result<OrderDto>.BadRequest("Order must contain at least one item.");

        var pizzaIds = items.Select(i => i.PizzaId).Distinct().ToList();
        var pizzas = await db.Pizzas.Where(p => pizzaIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        if (pizzas.Count != pizzaIds.Count)
            return Result<OrderDto>.BadRequest("One or more pizzas not found.");

        var total = items.Sum(i => pizzas[i.PizzaId].Price * i.Quantity);
        var order = new Order
        {
            CustomerName = customerName,
            Status = OrderStatus.Received,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TotalPrice = total,
            Items = items.Select(i => new OrderItem { PizzaId = i.PizzaId, Quantity = i.Quantity }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Reload with navigation properties so DTO has pizza names
        var saved = await LoadOrderAsync(order.Id);
        var dto = saved!.ToDto();

        await hub.Clients.Group(nameof(HubGroup.Chef)).SendAsync("OrderReceived", dto);
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("OrderStatusChanged", dto);

        return Result<OrderDto>.Ok(dto);
    }

    public async Task<OrderDto?> GetOrderAsync(int id)
    {
        var order = await LoadOrderAsync(id);
        return order?.ToDto();
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(string? statusFilter)
    {
        var query = db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, out var parsed))
            query = query.Where(o => o.Status == parsed);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(o => o.ToDto());
    }

    private Task<Order?> LoadOrderAsync(int id) =>
        db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .FirstOrDefaultAsync(o => o.Id == id);
}
