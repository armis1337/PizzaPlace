namespace PizzaPlace.Api.Models;

public class Pizza
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public List<PizzaIngredient> Ingredients { get; set; } = [];
}
