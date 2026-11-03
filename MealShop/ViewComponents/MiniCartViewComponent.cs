using Microsoft.AspNetCore.Mvc;
using MealShop.Services;

namespace MealShop.ViewComponents;

public class MiniCartViewComponent : ViewComponent
{
    private readonly CartService _cartService;

    public MiniCartViewComponent(CartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart);
    }
}
