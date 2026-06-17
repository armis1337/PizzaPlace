using PizzaPlace.Api.Models;

namespace PizzaPlace.Api.Services;

/// <summary>
/// Pure, stateless ingredient-demand math shared by the supervisor inventory view
/// (aggregate demand across orders) and the chef view (per-order fulfillability).
/// Shortage is always DERIVED from live data here — never stored.
/// </summary>
public static class ShortageCalculator
{
    /// <summary>
    /// Total quantity required per ingredient id across the given orders:
    /// sum of (QuantityRequired × item Quantity) for every pizza ingredient.
    /// Pass a single order to get that order's own requirements.
    /// Requires Items → Pizza → Ingredients to be loaded.
    /// </summary>
    public static Dictionary<int, int> ComputeDemand(IEnumerable<Order> orders)
    {
        var demand = new Dictionary<int, int>();
        foreach (var order in orders)
            foreach (var item in order.Items)
                foreach (var pi in item.Pizza.Ingredients)
                    demand[pi.IngredientId] =
                        demand.GetValueOrDefault(pi.IngredientId) + pi.QuantityRequired * item.Quantity;
        return demand;
    }

    /// <summary>
    /// How many distinct orders contribute demand for each ingredient id —
    /// powers the "across N orders" line in the supervisor shortage tooltip.
    /// </summary>
    public static Dictionary<int, int> ComputeContributingOrderCounts(IEnumerable<Order> orders)
    {
        var counts = new Dictionary<int, int>();
        foreach (var order in orders)
        {
            var ingredientIds = order.Items
                .SelectMany(i => i.Pizza.Ingredients.Select(pi => pi.IngredientId))
                .Distinct();
            foreach (var id in ingredientIds)
                counts[id] = counts.GetValueOrDefault(id) + 1;
        }
        return counts;
    }
}
