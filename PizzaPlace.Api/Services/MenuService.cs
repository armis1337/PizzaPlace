using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Data;
using PizzaPlace.Api.DTOs;

namespace PizzaPlace.Api.Services;

public class MenuService(AppDbContext db)
{
    public async Task<IEnumerable<PizzaDto>> GetMenuAsync() =>
        await db.Pizzas
            .Select(p => new PizzaDto(p.Id, p.Name, p.Description, p.Price, p.ImageUrl))
            .ToListAsync();
}
