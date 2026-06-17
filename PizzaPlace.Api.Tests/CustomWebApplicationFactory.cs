using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PizzaPlace.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(), $"pizzaplace_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["Restock:DelaySeconds"] = "0"   // near-zero for fast tests
            }));
    }

    // Helper: create a client pre-loaded with a JWT for the given seeded role
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new { username, password = username });
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", data!.Token);
        return client;
    }

    public static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var f = _dbPath + suffix;
            if (File.Exists(f)) try { File.Delete(f); } catch { /* best-effort */ }
        }
    }
}
