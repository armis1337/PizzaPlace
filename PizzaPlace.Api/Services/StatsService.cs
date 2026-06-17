using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.Models.Enums;
using PizzaPlace.Api.DTOs;
using PizzaPlace.Api.Models;

namespace PizzaPlace.Api.Services;

public class StatsService(AppDbContext db)
{
    public async Task<StatsDto> GetSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var orders = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Pizza)
            .ToListAsync();

        var todayOrders   = orders.Where(o => o.CreatedAt.Date == today).ToList();
        var revenueToday  = todayOrders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalPrice);
        var revenueTotal  = orders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalPrice);

        var mostPopular = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Pizza.Name)
            .OrderByDescending(g => g.Sum(i => i.Quantity))
            .Select(g => new PopularPizzaDto(g.Key, g.Sum(i => i.Quantity)))
            .FirstOrDefault();

        return new StatsDto(
            OrdersToday:     todayOrders.Count,
            RevenueToday:    revenueToday,
            RevenueTotal:    revenueTotal,
            TotalOrders:     orders.Count,
            Delivered:       orders.Count(o => o.Status == OrderStatus.Delivered),
            MostPopularPizza: mostPopular);
    }
}
