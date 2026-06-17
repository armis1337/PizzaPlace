namespace PizzaPlace.Api.Models;

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = "";
    public int LowStockThreshold { get; set; }
    public bool IsRestocking { get; set; }
}
