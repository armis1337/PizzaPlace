namespace PizzaPlace.Api.DTOs;

public record IngredientDto(
    int Id,
    string Name,
    int StockQuantity,
    string Unit,
    int LowStockThreshold,
    bool IsRestocking,
    bool IsLow,
    int DemandFromOrders,
    bool HasShortage,
    int Deficit,
    int OrdersWithDemand);
