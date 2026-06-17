using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Tests.Tests;

public class ShortageCalculatorTests
{
    // Build an order from (ingredientId, qtyRequiredPerPizza, pizzaQuantity) specs.
    private static Order OrderWith(params (int IngredientId, int QtyRequired, int PizzaQty)[] specs) =>
        new()
        {
            Items = specs.Select(s => new OrderItem
            {
                Quantity = s.PizzaQty,
                Pizza = new Pizza
                {
                    Ingredients = { new PizzaIngredient { IngredientId = s.IngredientId, QuantityRequired = s.QtyRequired } }
                }
            }).ToList()
        };

    [Fact]
    public void ComputeDemand_TwoOrdersSameIngredient_SumsAcrossOrders()
    {
        // User's scenario: one order needs 100, another 80, of the same ingredient.
        var o1 = OrderWith((IngredientId: 1, QtyRequired: 100, PizzaQty: 1));
        var o2 = OrderWith((IngredientId: 1, QtyRequired: 80, PizzaQty: 1));

        var demand = ShortageCalculator.ComputeDemand(new[] { o1, o2 });

        Assert.Equal(180, demand[1]);
        // Against 150ml stock that's a shortage with deficit 30.
        const int stock = 150;
        Assert.True(demand[1] > stock);
        Assert.Equal(30, demand[1] - stock);
    }

    [Fact]
    public void ComputeDemand_MultipliesByPizzaQuantity_AndKeepsIngredientsSeparate()
    {
        var order = OrderWith(
            (IngredientId: 1, QtyRequired: 50, PizzaQty: 3),   // 150
            (IngredientId: 2, QtyRequired: 20, PizzaQty: 3));  // 60

        var demand = ShortageCalculator.ComputeDemand(new[] { order });

        Assert.Equal(150, demand[1]);
        Assert.Equal(60, demand[2]);
    }

    [Fact]
    public void ComputeDemand_NoOrders_ReturnsEmpty()
    {
        Assert.Empty(ShortageCalculator.ComputeDemand(Array.Empty<Order>()));
    }

    [Fact]
    public void ComputeContributingOrderCounts_CountsDistinctOrdersPerIngredient()
    {
        var o1 = OrderWith((IngredientId: 1, QtyRequired: 80, PizzaQty: 1));
        var o2 = OrderWith((IngredientId: 1, QtyRequired: 80, PizzaQty: 1),
                           (IngredientId: 2, QtyRequired: 50, PizzaQty: 1));

        var counts = ShortageCalculator.ComputeContributingOrderCounts(new[] { o1, o2 });

        Assert.Equal(2, counts[1]); // both orders use ingredient 1
        Assert.Equal(1, counts[2]); // only the second uses ingredient 2
    }
}
