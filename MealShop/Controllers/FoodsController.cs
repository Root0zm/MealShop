using Microsoft.AspNetCore.Mvc;
using MealShop.Services;
using MealShop.ViewModels;

namespace MealShop.Controllers;

public class FoodsController : Controller
{
    private readonly FoodQueryService _foodQueryService;

    public FoodsController(FoodQueryService foodQueryService)
    {
        _foodQueryService = foodQueryService;
    }

    // Danh sach thuc pham ho tro tim kiem, loc va phan trang
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? kind, string? category, int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        var pagedFoods = await _foodQueryService.SearchFoodsAsync(q, kind, category, page, pageSize: 20);
        var categories = await _foodQueryService.GetCategoriesAsync();

        var viewModel = new FoodsIndexViewModel
        {
            Q = q,
            Kind = kind,
            Category = category,
            AvailableCategories = categories,
            PagedFoods = pagedFoods
        };

        return View(viewModel);
    }

    // Xem chi tiet gia tri dinh duong va cong thuc cua mon an
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var food = await _foodQueryService.GetFoodDetailsAsync(id);
        if (food == null)
        {
            return NotFound();
        }

        return View(food);
    }
}
