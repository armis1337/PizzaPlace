using Microsoft.EntityFrameworkCore;
using PizzaPlace.Api.Models;
using PizzaPlace.Api.Models.Enums;

namespace PizzaPlace.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Clear any stuck restock flags from a previous crashed/restarted run
        await db.Ingredients.Where(i => i.IsRestocking)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsRestocking, false));

        if (await db.Users.AnyAsync()) return;

        var dough = new Ingredient { Name = "Pizza Dough", StockQuantity = 20, Unit = "balls", LowStockThreshold = 5 };
        var sauce = new Ingredient { Name = "Tomato Sauce", StockQuantity = 3000, Unit = "ml", LowStockThreshold = 500 };
        var mozz = new Ingredient { Name = "Mozzarella", StockQuantity = 4000, Unit = "g", LowStockThreshold = 800 };
        var pepp = new Ingredient { Name = "Pepperoni", StockQuantity = 2000, Unit = "g", LowStockThreshold = 400 };
        var fourCheese = new Ingredient { Name = "Four Cheese Blend", StockQuantity = 2400, Unit = "g", LowStockThreshold = 600 };
        var chili = new Ingredient { Name = "Chili Oil", StockQuantity = 500, Unit = "ml", LowStockThreshold = 100 };
        var nduja = new Ingredient { Name = "'Nduja Paste", StockQuantity = 800, Unit = "g", LowStockThreshold = 200 };
        var basil = new Ingredient { Name = "Fresh Basil", StockQuantity = 150, Unit = "g", LowStockThreshold = 30 };

        db.Ingredients.AddRange(dough, sauce, mozz, pepp, fourCheese, chili, nduja, basil);

        var margherita = new Pizza
        {
            Name = "Margherita",
            Description = "San Marzano tomato, fior di latte, fresh basil, extra virgin olive oil.",
            Price = 12.50m,
            ImageUrl = "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?w=600&q=80"
        };
        var pepperoni = new Pizza
        {
            Name = "Pepperoni",
            Description = "Tomato base, mozzarella, generous double-layer of spiced pepperoni.",
            Price = 14.50m,
            ImageUrl = "https://images.unsplash.com/photo-1628840042765-356cda07504e?w=600&q=80"
        };
        var quattro = new Pizza
        {
            Name = "Quattro Formaggi",
            Description = "White base, four-cheese blend — mozzarella, gorgonzola, fontina, parmigiano.",
            Price = 15.00m,
            ImageUrl = "https://images.unsplash.com/photo-1513104890138-7c749659a591?w=600&q=80"
        };
        var diavola = new Pizza
        {
            Name = "Diavola",
            Description = "Tomato, mozzarella, 'nduja, pepperoni, a drizzle of chili oil. Not for the faint-hearted.",
            Price = 15.50m,
            ImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=600&q=80"
        };

        db.Pizzas.AddRange(margherita, pepperoni, quattro, diavola);
        await db.SaveChangesAsync();

        db.PizzaIngredients.AddRange(
            new PizzaIngredient { PizzaId = margherita.Id, IngredientId = dough.Id, QuantityRequired = 1 },
            new PizzaIngredient { PizzaId = margherita.Id, IngredientId = sauce.Id, QuantityRequired = 80 },
            new PizzaIngredient { PizzaId = margherita.Id, IngredientId = mozz.Id, QuantityRequired = 150 },
            new PizzaIngredient { PizzaId = margherita.Id, IngredientId = basil.Id, QuantityRequired = 5 },

            new PizzaIngredient { PizzaId = pepperoni.Id, IngredientId = dough.Id, QuantityRequired = 1 },
            new PizzaIngredient { PizzaId = pepperoni.Id, IngredientId = sauce.Id, QuantityRequired = 80 },
            new PizzaIngredient { PizzaId = pepperoni.Id, IngredientId = mozz.Id, QuantityRequired = 150 },
            new PizzaIngredient { PizzaId = pepperoni.Id, IngredientId = pepp.Id, QuantityRequired = 80 },

            new PizzaIngredient { PizzaId = quattro.Id, IngredientId = dough.Id, QuantityRequired = 1 },
            new PizzaIngredient { PizzaId = quattro.Id, IngredientId = fourCheese.Id, QuantityRequired = 200 },

            new PizzaIngredient { PizzaId = diavola.Id, IngredientId = dough.Id, QuantityRequired = 1 },
            new PizzaIngredient { PizzaId = diavola.Id, IngredientId = sauce.Id, QuantityRequired = 80 },
            new PizzaIngredient { PizzaId = diavola.Id, IngredientId = mozz.Id, QuantityRequired = 120 },
            new PizzaIngredient { PizzaId = diavola.Id, IngredientId = nduja.Id, QuantityRequired = 40 },
            new PizzaIngredient { PizzaId = diavola.Id, IngredientId = pepp.Id, QuantityRequired = 40 },
            new PizzaIngredient { PizzaId = diavola.Id, IngredientId = chili.Id, QuantityRequired = 10 }
        );

        db.Users.AddRange(
            new AppUser { Username = "chef", PasswordHash = BCrypt.Net.BCrypt.HashPassword("chef"), Role = nameof(UserRole.Chef) },
            new AppUser { Username = "delivery", PasswordHash = BCrypt.Net.BCrypt.HashPassword("delivery"), Role = nameof(UserRole.Delivery) },
            new AppUser { Username = "supervisor", PasswordHash = BCrypt.Net.BCrypt.HashPassword("supervisor"), Role = nameof(UserRole.Supervisor) }
        );

        await db.SaveChangesAsync();
    }
}
