namespace PizzaPlace.Api.Models.Enums;

/// <summary>Authenticated roles only — written to JWT claims and used in [Authorize]. No Guest.</summary>
public enum UserRole
{
    Chef,
    Delivery,
    Supervisor
}
