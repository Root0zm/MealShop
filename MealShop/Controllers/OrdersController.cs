using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Extensions;
using MealShop.Models;
using MealShop.Services;
using MealShop.ViewModels;

namespace MealShop.Controllers;

public class OrdersController : Controller
{
    private readonly OrderService _orderService;
    private readonly CartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;

    public OrdersController(
        OrderService orderService,
        CartService cartService,
        UserManager<ApplicationUser> userManager,
        AppDbContext context)
    {
        _orderService = orderService;
        _cartService = cartService;
        _userManager = userManager;
        _context = context;
    }

    // Trang thanh toan / nhap thong tin nhan hang
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            this.FlashWarning("Bữa ăn của bạn chưa có món nào. Vui lòng chọn món trước khi đặt.");
            return RedirectToAction("Index", "Menu");
        }

        var model = new CheckoutViewModel
        {
            Cart = cart
        };

        // Dien san thong tin nguoi dung neu da dang nhap
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                model.CustomerName = user.FullName ?? string.Empty;
                model.PhoneNumber = user.PhoneNumber ?? string.Empty;
            }
        }

        return View(model);
    }

    // Xu ly dat hang (chong spam rate limit 5 req/phut)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("checkout-limit")]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var cart = await _cartService.GetCartAsync();
        if (cart.Items.Count == 0)
        {
            this.FlashWarning("Bữa ăn của bạn chưa có món nào.");
            return RedirectToAction("Index", "Menu");
        }

        if (cart.Warnings.Count > 0)
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng vừa thay đổi vì có món không còn bán. Vui lòng kiểm tra lại và xác nhận đặt hàng lần nữa.");
        }

        if (!ModelState.IsValid)
        {
            model.Cart = cart;
            return View(model);
        }

        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        try
        {
            var order = await _orderService.CreateOrderAsync(model, userId);
            HttpContext.Session.SetInt32("OrderConfirmation:" + order.PublicCode, order.Id);
            this.FlashSuccess("Đặt hàng thành công! Hãy lưu lại mã đơn hàng của bạn.");
            return RedirectToAction(nameof(Confirmation), new { publicCode = order.PublicCode });
        }
        catch (InvalidOperationException ex)
        {
            this.FlashWarning(ex.Message);
            model.Cart = await _cartService.GetCartAsync();
            return View(model);
        }
        catch (Exception)
        {
            this.FlashDanger("Chưa thể tạo đơn hàng. Vui lòng thử lại sau.");
            model.Cart = await _cartService.GetCartAsync();
            return View(model);
        }
    }

    // Trang xac nhan don hang kem ma cong khai
    [HttpGet]
    public async Task<IActionResult> Confirmation(string publicCode)
    {
        if (string.IsNullOrWhiteSpace(publicCode))
        {
            return NotFound();
        }

        var cleanCode = publicCode.Trim().ToUpperInvariant();
        var order = await _orderService.GetConfirmationAsync(cleanCode,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            HttpContext.Session.GetInt32("OrderConfirmation:" + cleanCode));

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // Danh sach don cua nguoi dung dang nhap
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var orders = await _orderService.GetOrdersByUserIdAsync(userId);
        return View(orders);
    }

    // Chi tiet don cua nguoi dung dang nhap (chi xem duoc don cua chinh minh)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var order = await _orderService.GetOrderForUserAsync(id, userId);
        if (order == null)
        {
            return NotFound();
        }

        var viewModel = new OrderDetailsViewModel
        {
            Order = order,
            CanReorder = true,
            IsAdmin = false
        };

        return View(viewModel);
    }

    // Trang tra cuu don hang cho khach vang lai (GET)
    [HttpGet]
    public IActionResult Track()
    {
        return View(new OrderTrackViewModel());
    }

    // Xu ly tra cuu don hang (POST, rate limit 10 req/phut)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("track-limit")]
    public async Task<IActionResult> Track(OrderTrackViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var order = await _orderService.TrackOrderAsync(model.PublicCode, model.PhoneNumber);
        if (order == null)
        {
            // Thong bao chung khong tiet lo sai ma hay sai sdt
            model.ErrorMessage = "Không tìm thấy đơn hàng. Vui lòng kiểm tra lại mã đơn và số điện thoại đã nhập.";
            return View(model);
        }

        model.Order = order;
        return View(model);
    }

    // Dat lai (Reorder)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int id, string? publicCode = null, string? phoneNumber = null)
    {
        var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            this.FlashDanger("Không tìm thấy đơn hàng yêu cầu.");
            return RedirectToAction("Index", "Menu");
        }

        // Kiem tra quyen: Nguoi dung dang nhap phai la chu don; khach vang lai phai khop ma va sdt
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool isAuthorized = false;

        if (!string.IsNullOrEmpty(userId) && order.UserId == userId)
        {
            isAuthorized = true;
        }
        else if (!string.IsNullOrEmpty(publicCode) && !string.IsNullOrEmpty(phoneNumber))
        {
            var tracked = await _orderService.TrackOrderAsync(publicCode, phoneNumber);
            if (tracked != null && tracked.Id == id)
            {
                isAuthorized = true;
            }
        }

        if (!isAuthorized)
        {
            this.FlashDanger("Bạn không có quyền đặt lại đơn hàng này.");
            return RedirectToAction(nameof(Track));
        }

        var result = await _orderService.ReorderAsync(id);

        if (!result.Success)
        {
            this.FlashWarning(result.Message);
            return RedirectToAction("Index", "Cart");
        }

        // Thong bao cac dong bi bo qua hoac thay doi gia
        if (result.SkippedItems.Count > 0 || result.PriceChangedItems.Count > 0)
        {
            this.FlashWarning(string.Join(" ", new[] { result.Message }
                .Concat(result.SkippedItems).Concat(result.PriceChangedItems)));
        }
        else
        {
            this.FlashSuccess(result.Message);
        }

        return RedirectToAction("Index", "Cart");
    }
}
