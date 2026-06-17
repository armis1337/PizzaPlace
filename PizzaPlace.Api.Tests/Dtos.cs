namespace PizzaPlace.Api.Tests;

record LoginResponse(string Token, string Role, string Username);
record OrderItemDto(int PizzaId, string PizzaName, int Quantity);
record OrderDto(int Id, string CustomerName, string Status, decimal TotalPrice,
    OrderItemDto[] Items, string? ClaimedByDeliveryUser, string? CancellationReason,
    bool? CanStart, string[]? BlockingIngredients);
record IngredientDto(int Id, string Name, int StockQuantity, string Unit,
    int LowStockThreshold, bool IsRestocking, bool IsLow,
    int DemandFromOrders, bool HasShortage, int Deficit, int OrdersWithDemand);
record PopularPizzaDto(string Pizza, int Count);
record StatsDto(int OrdersToday, decimal RevenueToday, decimal RevenueTotal,
    int TotalOrders, int Delivered, PopularPizzaDto? MostPopularPizza);
