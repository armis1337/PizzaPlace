namespace PizzaPlace.Api.DTOs;

public record PizzaDto(int Id, string Name, string Description, decimal Price, string? ImageUrl);
