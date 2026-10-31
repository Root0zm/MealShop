using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MealShop.Services;

namespace MealShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PortionsController : Controller
{
    private readonly DefaultPortionService _defaultPortionService;

    public PortionsController(DefaultPortionService defaultPortionService)
    {
        _defaultPortionService = defaultPortionService;
    }

    // Trang xem truoc quy tac tao khau phan mac dinh
    [HttpGet]
    public async Task<IActionResult> Defaults()
    {
        var preview = await _defaultPortionService.PreviewAsync();
        return View(preview);
    }

    // Ap dung tao khau phan mac dinh
    [HttpPost]
    public async Task<IActionResult> ApplyDefaults()
    {
        var result = await _defaultPortionService.ApplyAsync();
        TempData["SuccessMessage"] = $"Đã áp dụng thành công: tạo mới {result.CreatedCount} khẩu phần mặc định và kích hoạt bán. Bỏ qua {result.SkippedExistingCount} khẩu phần đã tồn tại và {result.SkippedSuspectCount} món nghi vấn chất lượng.";
        return RedirectToAction(nameof(Defaults));
    }
}
