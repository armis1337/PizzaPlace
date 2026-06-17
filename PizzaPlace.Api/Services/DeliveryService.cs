using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Common;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.DTOs;
using PizzaPlace.Api.Hubs;
using PizzaPlace.Api.Models.Enums;

namespace PizzaPlace.Api.Services;

public class DeliveryService(AppDbContext db, IHubContext<OrderHub> hub)
{
    public async Task<IEnumerable<OrderDto>> GetDeliveryOrdersAsync(string username)
    {
        var orders = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .Where(o => o.Status == OrderStatus.Ready ||
                        (o.Status == OrderStatus.OutForDelivery && o.ClaimedByDeliveryUser == username))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => o.ToDto());
    }

    public async Task<Result<OrderDto>> ClaimOrderAsync(int id, string username)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return Result<OrderDto>.NotFound();
        if (order.Status != OrderStatus.Ready)
            return Result<OrderDto>.BadRequest("Order must be Ready to claim.");

        order.Status = OrderStatus.OutForDelivery;
        order.ClaimedByDeliveryUser = username;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = order.ToDto();
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("OrderStatusChanged", dto);
        await hub.Clients.Group(nameof(HubGroup.Guest)).SendAsync("OrderStatusChanged", dto);

        return Result<OrderDto>.Ok(dto);
    }

    public async Task<Result<OrderDto>> DeliverOrderAsync(int id, string username)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return Result<OrderDto>.NotFound();
        if (order.Status != OrderStatus.OutForDelivery)
            return Result<OrderDto>.BadRequest("Order must be OutForDelivery to mark delivered.");
        if (order.ClaimedByDeliveryUser != username)
            return Result<OrderDto>.Forbidden();

        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = order.ToDto();
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("OrderStatusChanged", dto);
        await hub.Clients.Group(nameof(HubGroup.Guest)).SendAsync("OrderStatusChanged", dto);

        return Result<OrderDto>.Ok(dto);
    }
}
