using Microsoft.EntityFrameworkCore;

namespace PizzaPlace.Api.Tests.Tests;

public class KitchenTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private static readonly JsonSerializerOptions J = CustomWebApplicationFactory.JsonOpts;

    public KitchenTests() => _factory.CreateClient(); // force init + seeding
    public void Dispose() => _factory.Dispose();

    private async Task<OrderDto> PlaceOrderAsync(int pizzaId = 1, int quantity = 1)
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/orders", new
        {
            customerName = "Kitchen Test",
            items = new[] { new { pizzaId, quantity } }
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;
    }

    [Fact]
    public async Task StartOrder_DecrementsIngredientStock()
    {
        // Margherita (pizzaId=1) uses Mozzarella x150g and Dough x1
        var order = await PlaceOrderAsync(pizzaId: 1, quantity: 1);
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        int mozzBefore, doughBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            mozzBefore  = (await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella")).StockQuantity;
            doughBefore = (await db.Ingredients.FirstAsync(i => i.Name == "Pizza Dough")).StockQuantity;
        }

        var resp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(mozzBefore  - 150, (await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella")).StockQuantity);
            Assert.Equal(doughBefore - 1,   (await db.Ingredients.FirstAsync(i => i.Name == "Pizza Dough")).StockQuantity);
        }
    }

    [Fact]
    public async Task StartOrder_InsufficientStock_Returns400_NoPartialDeduction()
    {
        var order = await PlaceOrderAsync(pizzaId: 1, quantity: 1);

        // Drain Mozzarella to 0 — this is the only test using this DB so no cross-contamination
        int doughBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mozz = await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella");
            mozz.StockQuantity = 0;
            await db.SaveChangesAsync();
            doughBefore = (await db.Ingredients.FirstAsync(i => i.Name == "Pizza Dough")).StockQuantity;
        }

        var chef = await _factory.CreateAuthenticatedClientAsync("chef");
        var resp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/start", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(0, (await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella")).StockQuantity);
            Assert.Equal(doughBefore, (await db.Ingredients.FirstAsync(i => i.Name == "Pizza Dough")).StockQuantity);
        }
    }

    [Fact]
    public async Task StartOrder_MultiItemOrder_SharedIngredientInsufficient_Returns400_NoDeduction()
    {
        // Margherita (id=1) and Pepperoni (id=2) both use Tomato Sauce (80 ml each).
        // An order with both needs 160 ml total. Setting sauce to 100 ml is enough for
        // one pizza but not two — the old per-item check would have missed this and gone
        // negative on the second decrement; the aggregated check must reject it.
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Kitchen Test",
            items = new[]
            {
                new { pizzaId = 1, quantity = 1 }, // Margherita: 80 ml sauce, 150 g mozz
                new { pizzaId = 2, quantity = 1 }  // Pepperoni:  80 ml sauce, 150 g mozz
            }
        });
        resp.EnsureSuccessStatusCode();
        var order = (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;

        int mozzBefore;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sauce = await db.Ingredients.FirstAsync(i => i.Name == "Tomato Sauce");
            sauce.StockQuantity = 100; // enough for 1 pizza, not 2 (need 160)
            await db.SaveChangesAsync();
            mozzBefore = (await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella")).StockQuantity;
        }

        var chef = await _factory.CreateAuthenticatedClientAsync("chef");
        var startResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/start", null);
        Assert.Equal(HttpStatusCode.BadRequest, startResp.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Nothing was decremented — not even the ingredient that was sufficient
            Assert.Equal(100, (await db.Ingredients.FirstAsync(i => i.Name == "Tomato Sauce")).StockQuantity);
            Assert.Equal(mozzBefore, (await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella")).StockQuantity);
        }
    }

    [Fact]
    public async Task StartOrder_SequentialStarts_NeverGoNegative()
    {
        // Margherita uses Fresh Basil (5 g each). Set stock to 15 g → exactly 3 pizzas.
        // Place 4 orders and start them all sequentially — exactly 3 must succeed and
        // the 4th must be rejected; final stock must be 0, never negative.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var basil = await db.Ingredients.FirstAsync(i => i.Name == "Fresh Basil");
            basil.StockQuantity = 15;
            await db.SaveChangesAsync();
        }

        var chef = await _factory.CreateAuthenticatedClientAsync("chef");
        var orders = new List<OrderDto>();
        for (var i = 0; i < 4; i++)
            orders.Add(await PlaceOrderAsync(pizzaId: 1, quantity: 1));

        var successes = 0;
        foreach (var o in orders)
        {
            var startResp = await chef.PostAsync($"/api/kitchen/orders/{o.Id}/start", null);
            if (startResp.StatusCode == HttpStatusCode.OK) successes++;
        }

        Assert.Equal(3, successes);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var finalBasil = (await db.Ingredients.FirstAsync(i => i.Name == "Fresh Basil")).StockQuantity;
            Assert.True(finalBasil >= 0, $"Stock went negative: {finalBasil}");
            Assert.Equal(0, finalBasil);
        }
    }

    [Fact]
    public async Task MarkReady_OrderStillReceived_Returns400()
    {
        var order = await PlaceOrderAsync();
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        // Skip Preparing — jump straight to Ready — must be rejected
        var resp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/ready", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ReceivedOrder_Returns200_StatusCancelled()
    {
        var order = await PlaceOrderAsync();
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        var resp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var cancelled = (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Cancelled", cancelled.Status);
    }

    [Fact]
    public async Task CancelOrder_NotReceived_Returns400_StatusUnchanged()
    {
        var order = await PlaceOrderAsync();
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        // Move to Preparing first — a Preparing order must not be cancellable
        var startResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var cancelResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, cancelResp.StatusCode);

        // Status stays Preparing
        var fetched = (await (await _factory.CreateClient().GetAsync($"/api/orders/{order.Id}"))
            .Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Preparing", fetched.Status);
    }

    [Fact]
    public async Task CancelOrder_RemovedFromKitchenQueue_ButStillInAllOrders()
    {
        var order = await PlaceOrderAsync();
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        var cancelResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        // No longer on the chef's active queue
        var queue = (await (await chef.GetAsync("/api/kitchen/orders"))
            .Content.ReadFromJsonAsync<OrderDto[]>(J))!;
        Assert.DoesNotContain(queue, o => o.Id == order.Id);

        // Still present in the supervisor's all-orders / history view
        var supervisor = await _factory.CreateAuthenticatedClientAsync("supervisor");
        var all = (await (await supervisor.GetAsync("/api/orders"))
            .Content.ReadFromJsonAsync<OrderDto[]>(J))!;
        var found = Assert.Single(all, o => o.Id == order.Id);
        Assert.Equal("Cancelled", found.Status);
    }

    [Fact]
    public async Task CancelOrder_ExcludedFromRevenue_ButCountedInTotalOrders()
    {
        var order = await PlaceOrderAsync(pizzaId: 1, quantity: 2); // €25.00 if it were fulfilled
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");
        var supervisor = await _factory.CreateAuthenticatedClientAsync("supervisor");

        var cancelResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var stats = (await (await supervisor.GetAsync("/api/stats/summary"))
            .Content.ReadFromJsonAsync<StatsDto>(J))!;

        // Never fulfilled → contributes nothing to revenue, but still a real order in the totals
        Assert.Equal(0m, stats.RevenueTotal);
        Assert.Equal(0m, stats.RevenueToday);
        Assert.True(stats.TotalOrders >= 1, $"Expected the cancelled order to still count; got {stats.TotalOrders}");
    }

    [Fact]
    public async Task CancelOrder_WithReason_StoresAndReturnsReason()
    {
        var order = await PlaceOrderAsync();
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        var resp = await chef.PostAsJsonAsync($"/api/kitchen/orders/{order.Id}/cancel",
            new { reason = "Out of basil" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var cancelled = (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal("Out of basil", cancelled.CancellationReason);

        // Reason persists — visible on the public order-tracking GET the guest uses
        var fetched = (await (await _factory.CreateClient().GetAsync($"/api/orders/{order.Id}"))
            .Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Out of basil", fetched.CancellationReason);
    }

    [Fact]
    public async Task CancelOrder_WithoutReason_CancelsWithNullReason()
    {
        var order = await PlaceOrderAsync();
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        // Empty/blank reason must not block cancellation and is stored as null
        var resp = await chef.PostAsJsonAsync($"/api/kitchen/orders/{order.Id}/cancel",
            new { reason = "   " });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var cancelled = (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Null(cancelled.CancellationReason);
    }

    // ── Per-order canStart (dynamic fulfillability against live stock) ──────────

    private async Task<OrderDto[]> GetKitchenOrdersAsync(HttpClient chef) =>
        (await (await chef.GetAsync("/api/kitchen/orders"))
            .Content.ReadFromJsonAsync<OrderDto[]>(J))!;

    private async Task SetStockAsync(string name, int qty)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Ingredients.FirstAsync(i => i.Name == name)).StockQuantity = qty;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task KitchenOrders_OrderSatisfiable_CanStartTrue()
    {
        var order = await PlaceOrderAsync(pizzaId: 1, quantity: 1);
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        var fetched = (await GetKitchenOrdersAsync(chef)).Single(o => o.Id == order.Id);
        Assert.True(fetched.CanStart);
        Assert.Empty(fetched.BlockingIngredients!);
    }

    [Fact]
    public async Task KitchenOrders_OrderExceedsStock_CanStartFalse_WithBlockingIngredient()
    {
        var order = await PlaceOrderAsync(pizzaId: 1, quantity: 1); // needs 80 ml sauce
        await SetStockAsync("Tomato Sauce", 50);
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        var fetched = (await GetKitchenOrdersAsync(chef)).Single(o => o.Id == order.Id);
        Assert.False(fetched.CanStart);
        Assert.Contains("Tomato Sauce", fetched.BlockingIngredients!);
    }

    [Fact]
    public async Task KitchenOrders_AfterRestock_CanStartFlipsTrue()
    {
        var order = await PlaceOrderAsync(pizzaId: 1, quantity: 1);
        await SetStockAsync("Tomato Sauce", 50);
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        Assert.False((await GetKitchenOrdersAsync(chef)).Single(o => o.Id == order.Id).CanStart);

        await SetStockAsync("Tomato Sauce", 3000); // restock

        Assert.True((await GetKitchenOrdersAsync(chef)).Single(o => o.Id == order.Id).CanStart);
    }

    [Fact]
    public async Task KitchenOrders_DynamicSequential_StartingOneDisablesOthers()
    {
        // 150 ml sauce; three Margherita orders each needing 80 ml. Each fits on its own,
        // so all three start as canStart=true. Starting one drops stock to 70 ml, leaving
        // both remaining orders un-startable (80 > 70) — recomputed against live stock.
        await SetStockAsync("Tomato Sauce", 150);
        var o1 = await PlaceOrderAsync(pizzaId: 1, quantity: 1);
        var o2 = await PlaceOrderAsync(pizzaId: 1, quantity: 1);
        var o3 = await PlaceOrderAsync(pizzaId: 1, quantity: 1);
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        var before = await GetKitchenOrdersAsync(chef);
        Assert.True(before.Single(o => o.Id == o1.Id).CanStart);
        Assert.True(before.Single(o => o.Id == o2.Id).CanStart);
        Assert.True(before.Single(o => o.Id == o3.Id).CanStart);

        // Start the first — stock 150 → 70
        (await chef.PostAsync($"/api/kitchen/orders/{o1.Id}/start", null)).EnsureSuccessStatusCode();

        var after = await GetKitchenOrdersAsync(chef);
        Assert.False(after.Single(o => o.Id == o2.Id).CanStart);
        Assert.False(after.Single(o => o.Id == o3.Id).CanStart);
    }
}
