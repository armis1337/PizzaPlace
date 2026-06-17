namespace PizzaPlace.Api.DTOs;

public record LoginResponseDto(string Token, string Role, string Username);
