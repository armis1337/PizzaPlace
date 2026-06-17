using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Common;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.DTOs;

namespace PizzaPlace.Api.Services;

public class AuthService(AppDbContext db, TokenService tokens)
{
    public async Task<Result<LoginResponseDto>> LoginAsync(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Result<LoginResponseDto>.Unauthorized("Invalid credentials.");

        return Result<LoginResponseDto>.Ok(
            new LoginResponseDto(tokens.GenerateToken(user), user.Role, user.Username));
    }
}
