namespace PizzaPlace.Api.Models.Enums;

/// <summary>SignalR connection group names. Includes Guest because unauthenticated clients join the hub too.</summary>
public enum HubGroup
{
    Guest,
    Chef,
    Delivery,
    Supervisor
}
