using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.Common;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.DTOs;
using PizzaPlace.Api.Hubs;

namespace PizzaPlace.Api.Services;

public class InventoryService(
    AppDbContext db,
    IHubContext<OrderHub> hub,
    IServiceScopeFactory scopeFactory,
    IConfiguration config)
{
    public async Task<IEnumerable<IngredientDto>> GetInventoryAsync()
    {
        var ingredients = await db.Ingredients.ToListAsync();

        // Aggregate demand from all un-started (Received) orders only — Preparing already
        // decremented stock, and Cancelled/later states never will. Derived, never stored.
        var receivedOrders = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza).ThenInclude(p => p.Ingredients)
            .Where(o => o.Status == OrderStatus.Received)
            .ToListAsync();
        var demand = ShortageCalculator.ComputeDemand(receivedOrders);
        var orderCounts = ShortageCalculator.ComputeContributingOrderCounts(receivedOrders);

        return ingredients.Select(i =>
        {
            var d = demand.GetValueOrDefault(i.Id);
            return new IngredientDto(
                i.Id, i.Name, i.StockQuantity, i.Unit,
                i.LowStockThreshold, i.IsRestocking,
                IsLow: i.StockQuantity <= i.LowStockThreshold,
                DemandFromOrders: d,
                HasShortage: d > i.StockQuantity,
                Deficit: Math.Max(0, d - i.StockQuantity),
                OrdersWithDemand: orderCounts.GetValueOrDefault(i.Id));
        });
    }

    public async Task<Result<string>> RestockAsync(int id)
    {
        var ingredient = await db.Ingredients.FindAsync(id);
        if (ingredient is null) return Result<string>.NotFound();
        if (ingredient.IsRestocking)
            return Result<string>.BadRequest("Already restocking.");

        ingredient.IsRestocking = true;
        await db.SaveChangesAsync();

        await hub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("InventoryChanged", new { });
        await hub.Clients.Group(nameof(HubGroup.Chef)).SendAsync("InventoryChanged", new { });

        var delay = TimeSpan.FromSeconds(config.GetValue<double>("Restock:DelaySeconds", 5));
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay);

            using var scope = scopeFactory.CreateScope();
            var scopedDb  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scopedHub = scope.ServiceProvider.GetRequiredService<IHubContext<OrderHub>>();

            var ing = await scopedDb.Ingredients.FindAsync(id);
            if (ing is not null)
            {
                ing.StockQuantity += ing.Unit == "balls" ? 20 : ing.Unit == "ml" ? 2000 : 1000;
                ing.IsRestocking = false;
                await scopedDb.SaveChangesAsync();
                await scopedHub.Clients.Group(nameof(HubGroup.Supervisor)).SendAsync("InventoryChanged", new { });
                await scopedHub.Clients.Group(nameof(HubGroup.Chef)).SendAsync("InventoryChanged", new { });
            }
        });

        return Result<string>.Ok("Restock started. Completes in ~5 seconds.");
    }
}
