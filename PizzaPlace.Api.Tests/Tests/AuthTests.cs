namespace PizzaPlace.Api.Tests.Tests;

public class AuthTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions J = CustomWebApplicationFactory.JsonOpts;

    public AuthTests() => _client = _factory.CreateClient();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task NoToken_ProtectedEndpoint_Returns401()
    {
        var resp = await _client.GetAsync("/api/kitchen/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task WrongRole_Returns403()
    {
        var deliveryClient = await _factory.CreateAuthenticatedClientAsync("delivery");
        var resp = await deliveryClient.GetAsync("/api/inventory");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "chef", password = "chef" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = (await resp.Content.ReadFromJsonAsync<LoginResponse>(J))!;
        Assert.False(string.IsNullOrWhiteSpace(data.Token));
        Assert.Equal("Chef", data.Role);
        Assert.Equal("chef", data.Username);
    }

    [Fact]
    public async Task Login_BadCredentials_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "chef", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
