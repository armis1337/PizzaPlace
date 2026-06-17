using Microsoft.EntityFrameworkCore;

namespace PizzaPlace.Api.Tests.Tests;

public class InventoryTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private static readonly JsonSerializerOptions J = CustomWebApplicationFactory.JsonOpts;

    public InventoryTests() => _factory.CreateClient();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetInventory_ReturnsSeedIngredients_NoneStartLow()
    {
        var supervisor = await _factory.CreateAuthenticatedClientAsync("supervisor");
        var resp = await supervisor.GetAsync("/api/inventory");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var ingredients = (await resp.Content.ReadFromJsonAsync<IngredientDto[]>(J))!;
        Assert.NotEmpty(ingredients);
        Assert.All(ingredients, i => Assert.False(i.IsLow,
            $"{i.Name}: stock={i.StockQuantity} should be above threshold={i.LowStockThreshold} on fresh seed"));
    }

    [Fact]
    public async Task Restock_ResponseMessage_MentionsFiveSeconds()
    {
        var supervisor = await _factory.CreateAuthenticatedClientAsync("supervisor");
        var all = (await (await supervisor.GetAsync("/api/inventory")).Content.ReadFromJsonAsync<IngredientDto[]>(J))!;
        var mozz = all.First(i => i.Name == "Mozzarella");

        var resp = await supervisor.PostAsync($"/api/inventory/{mozz.Id}/restock", null);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("~5 seconds", body); // not "~15 seconds"
    }

    [Fact]
    public async Task Restock_CompletesAndIncreasesStock()
    {
        var supervisor = await _factory.CreateAuthenticatedClientAsync("supervisor");

        // Get baseline stock for Mozzarella
        var listResp = await supervisor.GetAsync("/api/inventory");
        var all = (await listResp.Content.ReadFromJsonAsync<IngredientDto[]>(J))!;
        var mozz = all.First(i => i.Name == "Mozzarella");
        var stockBefore = mozz.StockQuantity;

        // Trigger restock (delay is 0 in tests)
        var restockResp = await supervisor.PostAsync($"/api/inventory/{mozz.Id}/restock", null);
        Assert.Equal(HttpStatusCode.OK, restockResp.StatusCode);

        // Poll until background task completes (should be near-instant with 0s delay)
        var deadline = DateTime.UtcNow.AddSeconds(5);
        bool completed = false;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ing = await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella");
            if (!ing.IsRestocking) { completed = true; break; }
        }

        Assert.True(completed, "Restock did not complete within 5 seconds");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ing = await db.Ingredients.FirstAsync(i => i.Name == "Mozzarella");
            Assert.False(ing.IsRestocking);
            Assert.True(ing.StockQuantity > stockBefore,
                $"Expected stock > {stockBefore}, got {ing.StockQuantity}");
        }
    }

    // ── Shortage warnings (derived from Received-order demand) ──────────────────

    private async Task PlaceMargheritaAsync(int quantity = 1)
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/orders", new
        {
            customerName = "Inv Test",
            items = new[] { new { pizzaId = 1, quantity } } // Margherita: 80 ml sauce
        });
        resp.EnsureSuccessStatusCode();
    }

    private async Task SetStockAsync(string name, int qty)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Ingredients.FirstAsync(i => i.Name == name)).StockQuantity = qty;
        await db.SaveChangesAsync();
    }

    private async Task<IngredientDto> GetIngredientAsync(string name)
    {
        var sup = await _factory.CreateAuthenticatedClientAsync("supervisor");
        var all = (await (await sup.GetAsync("/api/inventory")).Content.ReadFromJsonAsync<IngredientDto[]>(J))!;
        return all.First(i => i.Name == name);
    }

    [Fact]
    public async Task Inventory_SingleReceivedOrder_ExceedsStock_FlagsShortage()
    {
        await SetStockAsync("Tomato Sauce", 50);
        await PlaceMargheritaAsync(); // needs 80 ml

        var sauce = await GetIngredientAsync("Tomato Sauce");
        Assert.Equal(80, sauce.DemandFromOrders);
        Assert.True(sauce.HasShortage);
        Assert.Equal(30, sauce.Deficit); // 80 demand − 50 stock
    }

    [Fact]
    public async Task Inventory_TwoReceivedOrders_CombinedExceedsStock_DeficitIsSum()
    {
        // Each order (80 ml) fits in 130 ml on its own, but together (160) they exceed it.
        await SetStockAsync("Tomato Sauce", 130);
        await PlaceMargheritaAsync();
        await PlaceMargheritaAsync();

        var sauce = await GetIngredientAsync("Tomato Sauce");
        Assert.Equal(160, sauce.DemandFromOrders);
        Assert.True(sauce.HasShortage);
        Assert.Equal(30, sauce.Deficit); // 160 − 130
        Assert.Equal(2, sauce.OrdersWithDemand); // both orders contribute
    }

    [Fact]
    public async Task Inventory_DemandWithinStock_NoShortage()
    {
        await PlaceMargheritaAsync(); // 80 ml against the seeded 3000 ml

        var sauce = await GetIngredientAsync("Tomato Sauce");
        Assert.Equal(80, sauce.DemandFromOrders);
        Assert.False(sauce.HasShortage);
        Assert.Equal(0, sauce.Deficit);
    }

    [Fact]
    public async Task Inventory_CancelledAndPreparingOrders_DoNotCountTowardDemand()
    {
        var chef = await _factory.CreateAuthenticatedClientAsync("chef");

        // Order A → cancelled
        var a = await PlaceMargheritaReturningAsync();
        await chef.PostAsync($"/api/kitchen/orders/{a.Id}/cancel", null);

        // Order B → started (Preparing); decrements stock but must not count as demand
        var b = await PlaceMargheritaReturningAsync();
        (await chef.PostAsync($"/api/kitchen/orders/{b.Id}/start", null)).EnsureSuccessStatusCode();

        // Order C → left Received; the only one that should contribute demand
        await PlaceMargheritaAsync();

        await SetStockAsync("Tomato Sauce", 50);

        var sauce = await GetIngredientAsync("Tomato Sauce");
        Assert.Equal(80, sauce.DemandFromOrders); // only order C, not A or B
        Assert.True(sauce.HasShortage);
        Assert.Equal(30, sauce.Deficit);
    }

    [Fact]
    public async Task Inventory_AfterStockIncrease_ShortageClears()
    {
        await SetStockAsync("Tomato Sauce", 50);
        await PlaceMargheritaAsync();
        Assert.True((await GetIngredientAsync("Tomato Sauce")).HasShortage);

        await SetStockAsync("Tomato Sauce", 2000); // restock lifts stock past demand

        var sauce = await GetIngredientAsync("Tomato Sauce");
        Assert.False(sauce.HasShortage);
        Assert.Equal(0, sauce.Deficit);
    }

    private async Task<OrderDto> PlaceMargheritaReturningAsync()
    {
        var resp = await _factory.CreateClient().PostAsJsonAsync("/api/orders", new
        {
            customerName = "Inv Test",
            items = new[] { new { pizzaId = 1, quantity = 1 } }
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;
    }
}
