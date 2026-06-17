using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Models;

namespace PizzaPlace.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Pizza> Pizzas => Set<Pizza>();
    public DbSet<PizzaIngredient> PizzaIngredients => Set<PizzaIngredient>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<PizzaIngredient>().HasKey(pi => new { pi.PizzaId, pi.IngredientId });
    }
}
