using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/auth")]
public class AuthController(AuthService auth) : ApiController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req) =>
        ToResult(await auth.LoginAsync(req.Username, req.Password));
}

public record LoginRequest(string Username, string Password);
