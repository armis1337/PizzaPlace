namespace PizzaPlace.Api.Tests.Tests;

public class OrderingTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions J = CustomWebApplicationFactory.JsonOpts;

    public OrderingTests() => _client = _factory.CreateClient();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task PlaceOrder_ValidRequest_Returns200_WithReceivedStatus_CorrectTotal()
    {
        var resp = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Alice",
            items = new[] { new { pizzaId = 1, quantity = 2 } } // Margherita x2 = €25.00
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var order = await resp.Content.ReadFromJsonAsync<OrderDto>(J);
        Assert.NotNull(order);
        Assert.Equal("Received", order.Status);
        Assert.Equal("Alice", order.CustomerName);
        Assert.Equal(25.00m, order.TotalPrice);
        Assert.Single(order.Items);
        Assert.Equal(1, order.Items[0].PizzaId);
        Assert.Equal(2, order.Items[0].Quantity);
    }

    [Fact]
    public async Task PlaceOrder_EmptyCustomerName_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "",
            items = new[] { new { pizzaId = 1, quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_NoItems_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Bob",
            items = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_NonExistentPizzaId_Returns400()
    {
        var resp = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Charlie",
            items = new[] { new { pizzaId = 99999, quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ExistingId_ReturnsOrder()
    {
        var placeResp = await _client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Dave",
            items = new[] { new { pizzaId = 1, quantity = 1 } }
        });
        var placed = (await placeResp.Content.ReadFromJsonAsync<OrderDto>(J))!;

        var resp = await _client.GetAsync($"/api/orders/{placed.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var fetched = (await resp.Content.ReadFromJsonAsync<OrderDto>(J))!;
        Assert.Equal(placed.Id, fetched.Id);
        Assert.Equal("Received", fetched.Status);
    }

    [Fact]
    public async Task GetOrder_UnknownId_Returns404()
    {
        var resp = await _client.GetAsync("/api/orders/99999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
