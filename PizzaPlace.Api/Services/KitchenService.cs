using System.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Common;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.DTOs;
using PizzaPlace.Api.Hubs;
using PizzaPlace.Api.Models;
using PizzaPlace.Api.Models.Enums;

namespace PizzaPlace.Api.Services;

public class KitchenService(AppDbContext db, IHubContext<OrderHub> hub)
{
    public async Task<IEnumerable<OrderDto>> GetKitchenOrdersAsync()
    {
        var stock = await db.Ingredients.ToDictionaryAsync(i => i.Id);

        var orders = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza).ThenInclude(p => p.Ingredients)
            .Where(o => o.Status == OrderStatus.Received || o.Status == OrderStatus.Preparing)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        // Per-order, dynamic check: can THIS order be started against the stock remaining
        // right now? Independent per order (no reservation) — recomputed on every fetch, so
        // starting one order (which decrements stock) can flip others to not-startable.
        return orders.Select(o =>
        {
            var requirements = ShortageCalculator.ComputeDemand(new[] { o });
            var blocking = requirements
                .Where(r => !stock.TryGetValue(r.Key, out var ing) || ing.StockQuantity < r.Value)
                .Select(r => stock[r.Key].Name)
                .ToList();
            return o.ToDto() with { CanStart = blocking.Count == 0, BlockingIngredients = blocking };
        });
    }

    public async Task<Result<OrderDto>> StartOrderAsync(int id)
    {
        // Begin the transaction before reading so the check and decrement are atomic.
        using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
                .ThenInclude(p => p.Ingredients).ThenInclude(pi => pi.Ingredient)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return Result<OrderDto>.NotFound();
        if (order.Status != OrderStatus.Received)
            return Result<OrderDto>.BadRequest("Order must be in Received status to start.");

        // Aggregate total demand per ingredient across ALL items in the order.
        // Checking each (item, ingredient) pair independently against the original stock
        // misses the case where two pizzas share an ingredient — both checks read the same
        // unmodified value, so combined demand can silently exceed supply. The decrement
        // loop then drives the shared entity negative on the second iteration.
        var demand = new Dictionary<int, (Ingredient Ingredient, int TotalNeeded)>();
        foreach (var item in order.Items)
            foreach (var pi in item.Pizza.Ingredients)
            {
                var needed = pi.QuantityRequired * item.Quantity;
                if (demand.TryGetValue(pi.Ingredient.Id, out var existing))
                    demand[pi.Ingredient.Id] = (existing.Ingredient, existing.TotalNeeded + needed);
                else
                    demand[pi.Ingredient.Id] = (pi.Ingredient, needed);
            }

        var deficit = demand.Values
            .Where(d => d.Ingredient.StockQuantity < d.TotalNeeded)
            .Select(d => $"{d.Ingredient.Name}: need {d.TotalNeeded} {d.Ingredient.Unit}, have {d.Ingredient.StockQuantity}")
            .ToList();

        if (deficit.Count > 0)
            return Result<OrderDto>.BadRequest("Insufficient stock.", deficit);

        // Decrement by the aggregated amounts; commit in the same transaction.
        foreach (var (ingredient, totalNeeded) in demand.Values)
            ingredient.StockQuantity -= totalNeeded;

        order.Status = OrderStatus.Preparing;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var dto = order.ToDto();
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("OrderStatusChanged", dto);
        await hub.Clients.Group(nameof(HubGroup.Guest)).SendAsync("OrderStatusChanged", dto);
        // Stock dropped — both the supervisor (shortage) and chef (canStart) views must refresh.
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("InventoryChanged", new { });
        await hub.Clients.Group(nameof(HubGroup.Chef)).SendAsync("InventoryChanged", new { });

        return Result<OrderDto>.Ok(dto);
    }

    public async Task<Result<OrderDto>> CancelOrderAsync(int id, string? reason = null)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return Result<OrderDto>.NotFound();
        if (order.Status != OrderStatus.Received)
            return Result<OrderDto>.BadRequest("Only orders in Received status can be cancelled.");

        // Received → Cancelled. No stock was decremented at Received, so nothing to restore.
        // Reason is optional — keep cancelling frictionless; normalise blank input to null.
        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = order.ToDto();
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("OrderStatusChanged", dto);
        await hub.Clients.Group(nameof(HubGroup.Guest)).SendAsync("OrderStatusChanged", dto);

        return Result<OrderDto>.Ok(dto);
    }

    public async Task<Result<OrderDto>> MarkReadyAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return Result<OrderDto>.NotFound();
        if (order.Status != OrderStatus.Preparing)
            return Result<OrderDto>.BadRequest("Order must be in Preparing status to mark ready.");

        order.Status = OrderStatus.Ready;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = order.ToDto();
        await hub.Clients.Group(nameof(HubGroup.Delivery)).SendAsync("OrderReady", dto);
        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("OrderStatusChanged", dto);
        await hub.Clients.Group(nameof(HubGroup.Guest)).SendAsync("OrderStatusChanged", dto);

        return Result<OrderDto>.Ok(dto);
    }
}
