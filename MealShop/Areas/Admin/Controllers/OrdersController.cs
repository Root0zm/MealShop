using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Extensions;
using MealShop.Services;
using MealShop.ViewModels;

namespace MealShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly AppDbContext _context;
    private readonly OrderService _orderService;

    public OrdersController(AppDbContext context, OrderService orderService)
    {
        _context = context;
        _orderService = orderService;
    }

    // Danh sach don hang toan he thong
    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(o => o.Status == status);
        }

        ViewBag.CurrentStatus = status ?? "All";
        var orders = await query.ToListAsync();
        return View(orders);
    }

    // Chi tiet don hang cho Admin
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var viewModel = new OrderDetailsViewModel
        {
            Order = order,
            CanReorder = false,
            IsAdmin = true
        };

        return View(viewModel);
    }

    // Cap nhat trang thai don hang
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var validStatuses = new[] { "Pending", "Confirmed", "Preparing", "Completed", "Cancelled" };
        if (!validStatuses.Contains(status))
        {
            this.FlashDanger("Trạng thái đơn hàng không hợp lệ.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var success = await _orderService.UpdateOrderStatusAsync(id, status);
        if (success)
        {
            this.FlashSuccess($"Đã cập nhật đơn hàng #{id}: {MealShop.Models.OrderStatus.Label(status)}.");
        }
        else
        {
            this.FlashWarning("Không thể chuyển sang trạng thái này. Vui lòng kiểm tra lại trạng thái hiện tại của đơn.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
