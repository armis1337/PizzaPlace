namespace PizzaPlace.Api.Tests.Tests;

public class LifecycleTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private static readonly JsonSerializerOptions J = CustomWebApplicationFactory.JsonOpts;

    public LifecycleTests() => _factory.CreateClient();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task FullOrderLifecycle_PlaceToDelivered()
    {
        var guest    = _factory.CreateClient();
        var chef     = await _factory.CreateAuthenticatedClientAsync("chef");
        var delivery = await _factory.CreateAuthenticatedClientAsync("delivery");

        // 1. Guest places order
        var placeResp = await guest.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Lifecycle Test",
            items = new[] { new { pizzaId = 3, quantity = 1 } } // Quattro Formaggi
        });
        Assert.Equal(HttpStatusCode.OK, placeResp.StatusCode);
        var order = (await placeResp.Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Received", order.Status);

        // 2. Chef starts (Received → Preparing)
        var startResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        Assert.Equal("Preparing", (await startResp.Content.ReadFromJsonAsync<OrderDto>(J))!.Status);

        // 3. Chef marks ready (Preparing → Ready)
        var readyResp = await chef.PostAsync($"/api/kitchen/orders/{order.Id}/ready", null);
        Assert.Equal(HttpStatusCode.OK, readyResp.StatusCode);
        Assert.Equal("Ready", (await readyResp.Content.ReadFromJsonAsync<OrderDto>(J))!.Status);

        // 4. Delivery claims (Ready → OutForDelivery)
        var claimResp = await delivery.PostAsync($"/api/delivery/orders/{order.Id}/claim", null);
        Assert.Equal(HttpStatusCode.OK, claimResp.StatusCode);
        var claimed = (await claimResp.Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("OutForDelivery", claimed.Status);
        Assert.Equal("delivery", claimed.ClaimedByDeliveryUser);

        // 5. Delivery marks delivered (OutForDelivery → Delivered)
        var deliverResp = await delivery.PostAsync($"/api/delivery/orders/{order.Id}/deliver", null);
        Assert.Equal(HttpStatusCode.OK, deliverResp.StatusCode);
        Assert.Equal("Delivered", (await deliverResp.Content.ReadFromJsonAsync<OrderDto>(J))!.Status);

        // Confirm via public GET
        var final = (await (await guest.GetAsync($"/api/orders/{order.Id}"))
            .Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal("Delivered", final.Status);
    }
}
