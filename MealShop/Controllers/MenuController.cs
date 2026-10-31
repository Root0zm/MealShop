using Microsoft.AspNetCore.Mvc;
using MealShop.Services;

namespace MealShop.Controllers;

public class MenuController : Controller
{
    private readonly MenuService _menuService;

    public MenuController(MenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? tab, string? goal, string? search)
    {
        var model = await _menuService.GetMenuAsync(tab, goal, search);
        return View(model);
    }
}
