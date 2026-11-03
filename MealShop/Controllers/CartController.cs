using Microsoft.AspNetCore.Mvc;
using MealShop.Extensions;
using MealShop.Services;

namespace MealShop.Controllers;

public class CartController : Controller
{
    private readonly CartService _cartService;

    public CartController(CartService cartService)
    {
        _cartService = cartService;
    }

    // Xem trang gio hang chi tiet
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart);
    }

    // Lay ViewComponent MiniCart cho cap nhat khong reload
    [HttpGet]
    public IActionResult MiniCart()
    {
        return ViewComponent("MiniCart");
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
            || Request.Headers.Accept.ToString().Contains("application/json");
    }

    // Them mon vao gio hang
    [HttpPost]
    public async Task<IActionResult> Add(int portionId, int quantity = 1, string? returnUrl = null)
    {
        try
        {
            await _cartService.AddToCartAsync(portionId, quantity);
        }
        catch (InvalidOperationException ex)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = ex.Message, type = "warning" });
            this.FlashWarning(ex.Message);
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Menu");
        }

        if (IsAjaxRequest())
        {
            return Json(new { success = true, message = "Đã thêm món vào bữa ăn của bạn.", type = "success" });
        }

        this.FlashSuccess("Đã thêm món vào bữa ăn của bạn.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Menu");
    }

    // Cap nhat so luong
    [HttpPost]
    public async Task<IActionResult> Update(int portionId, int quantity, string? returnUrl = null)
    {
        await _cartService.UpdateQuantityAsync(portionId, quantity);

        if (IsAjaxRequest())
        {
            return Json(new { success = true, message = "Đã cập nhật số lượng món.", type = "success" });
        }

        this.FlashSuccess("Đã cập nhật số lượng món.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // Xoa mon khoi gio hang
    [HttpPost]
    public async Task<IActionResult> Remove(int portionId, string? returnUrl = null)
    {
        await _cartService.RemoveFromCartAsync(portionId);

        if (IsAjaxRequest())
        {
            return Json(new { success = true, message = "Đã xóa món khỏi bữa ăn.", type = "warning" });
        }

        this.FlashWarning("Đã xóa món khỏi bữa ăn.");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // Xoa sach gio hang
    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        await _cartService.ClearCartAsync();

        if (IsAjaxRequest())
        {
            return Json(new { success = true, message = "Đã xóa tất cả món trong bữa ăn.", type = "warning" });
        }

        this.FlashWarning("Đã xóa tất cả món trong bữa ăn.");
        return RedirectToAction(nameof(Index));
    }
}
