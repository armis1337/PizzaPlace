namespace PizzaPlace.Api.DTOs;

public record PopularPizzaDto(string Pizza, int Count);

public record StatsDto(
    int OrdersToday,
    decimal RevenueToday,
    decimal RevenueTotal,
    int TotalOrders,
    int Delivered,
    PopularPizzaDto? MostPopularPizza);
