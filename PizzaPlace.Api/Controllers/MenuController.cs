using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaPlace.Api.Services;

namespace PizzaPlace.Api.Controllers;

[Route("api/menu")]
[AllowAnonymous]
public class MenuController(MenuService menu) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> GetMenu() => Ok(await menu.GetMenuAsync());
}
