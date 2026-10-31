using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;
using MealShop.ViewModels;

namespace MealShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FoodsController : Controller
{
    private readonly AppDbContext _context;
    private readonly FoodQueryService _queryService;
    private readonly AdminFoodService _adminFoodService;
    private readonly AdminRecipeService _adminRecipeService;
    private readonly NutritionCalculatorService _nutritionCalculator;

    public FoodsController(
        AppDbContext context,
        FoodQueryService queryService,
        AdminFoodService adminFoodService,
        AdminRecipeService adminRecipeService,
        NutritionCalculatorService nutritionCalculator)
    {
        _context = context;
        _queryService = queryService;
        _adminFoodService = adminFoodService;
        _adminRecipeService = adminRecipeService;
        _nutritionCalculator = nutritionCalculator;
    }

    // Danh sach quan tri thuc pham / mon an
    public async Task<IActionResult> Index(
        string? search,
        string? kind,
        string? category,
        bool? isSuspect,
        bool? isSellable,
        int page = 1)
    {
        var pagedFoods = await _queryService.SearchFoodsAsync(
            q: search,
            kind: kind,
            category: category,
            page: page,
            pageSize: 20,
            isSuspect: isSuspect,
            isSellable: isSellable);

        var categories = await _queryService.GetCategoriesAsync();

        var viewModel = new FoodsIndexViewModel
        {
            Q = search,
            Kind = kind,
            Category = category,
            IsSuspect = isSuspect,
            IsSellable = isSellable,
            AvailableCategories = categories,
            PagedFoods = pagedFoods
        };

        return View(viewModel);
    }

    // Form them moi thuc pham / mon an
    [HttpGet]
    public IActionResult Create()
    {
        var model = new AdminFoodFormViewModel
        {
            Kind = "dish",
            UsableAsComponent = true,
            IsSellable = true
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AdminFoodFormViewModel model)
    {
        if (model.Kind == "ingredient")
        {
            model.UsableAsComponent = true;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var food = await _adminFoodService.CreateFoodAsync(model);
        TempData["SuccessMessage"] = $"Them moi thanh cong: {food.NameVi} (Ma: {food.Id}).";

        if (food.Kind == "dish")
        {
            return RedirectToAction(nameof(Edit), new { id = food.Id });
        }

        return RedirectToAction(nameof(Index));
    }

    // Form chinh sua thuc pham / mon an
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var food = await _context.Foods
            .Include(f => f.Portions)
            .Include(f => f.Recipes)
                .ThenInclude(r => r.Component)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (food == null)
        {
            return NotFound();
        }

        var model = new AdminFoodFormViewModel
        {
            Id = food.Id,
            Kind = food.Kind,
            UsableAsComponent = food.UsableAsComponent,
            IsSellable = food.IsSellable,
            NameVi = food.NameVi,
            NameEn = food.NameEn,
            CategoryVi = food.CategoryVi,
            Kcal = food.Kcal,
            Water = food.Water,
            Protein = food.Protein,
            Fat = food.Fat,
            Carb = food.Carb,
            Fiber = food.Fiber,
            Cholesterol = food.Cholesterol,
            Calcium = food.Calcium,
            Phosphorus = food.Phosphorus,
            Iron = food.Iron,
            Sodium = food.Sodium,
            Potassium = food.Potassium,
            Zinc = food.Zinc,
            VitaminA = food.VitaminA,
            BetaCarotene = food.BetaCarotene,
            VitaminC = food.VitaminC,
            EnergyFlag = food.EnergyFlag,
            QaNote = food.QaNote,
            IsSuspect = food.IsSuspect
        };

        ViewBag.Food = food;

        if (food.Kind == "dish")
        {
            ViewBag.CalculatedNutrition = _nutritionCalculator.CalculateDishNutrition(food, food.Recipes.ToList());
            ViewBag.UsableComponents = await _adminRecipeService.GetUsableComponentsAsync(food.Id);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AdminFoodFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (model.Kind == "ingredient")
        {
            model.UsableAsComponent = true;
        }

        if (!ModelState.IsValid)
        {
            var food = await _context.Foods
                .Include(f => f.Portions)
                .Include(f => f.Recipes)
                    .ThenInclude(r => r.Component)
                .FirstOrDefaultAsync(f => f.Id == id);
            ViewBag.Food = food;
            if (food != null && food.Kind == "dish")
            {
                ViewBag.CalculatedNutrition = _nutritionCalculator.CalculateDishNutrition(food, food.Recipes.ToList());
                ViewBag.UsableComponents = await _adminRecipeService.GetUsableComponentsAsync(food.Id);
            }
            return View(model);
        }

        await _adminFoodService.UpdateFoodAsync(id, model);
        TempData["SuccessMessage"] = $"Cap nhat thanh cong: {model.NameVi}.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // Them thanh phan vao cong thuc
    [HttpPost]
    public async Task<IActionResult> AddComponent(int dishId, int componentId, decimal grams)
    {
        var (succeeded, message) = await _adminRecipeService.AddComponentAsync(dishId, componentId, grams);
        if (succeeded)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Edit), new { id = dishId });
    }

    // Cap nhat khoi luong thanh phan
    [HttpPost]
    public async Task<IActionResult> UpdateComponent(int dishId, int componentId, decimal grams)
    {
        var (succeeded, message) = await _adminRecipeService.UpdateComponentGramsAsync(dishId, componentId, grams);
        if (succeeded)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Edit), new { id = dishId });
    }

    // Xoa thanh phan khoi cong thuc
    [HttpPost]
    public async Task<IActionResult> RemoveComponent(int dishId, int componentId)
    {
        var (succeeded, message) = await _adminRecipeService.RemoveComponentAsync(dishId, componentId);
        if (succeeded)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Edit), new { id = dishId });
    }

    // Them khau phan moi
    [HttpPost]
    public async Task<IActionResult> AddPortion(int foodId, string name, decimal grams, decimal price, bool isActive = true)
    {
        var (succeeded, message) = await _adminRecipeService.AddPortionAsync(foodId, name, grams, price, isActive);
        if (succeeded)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Edit), new { id = foodId });
    }

    // Cap nhat khau phan
    [HttpPost]
    public async Task<IActionResult> UpdatePortion(int portionId, int foodId, string name, decimal grams, decimal price, bool isActive)
    {
        var (succeeded, message) = await _adminRecipeService.UpdatePortionAsync(portionId, name, grams, price, isActive);
        if (succeeded)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Edit), new { id = foodId });
    }

    // Xoa khau phan
    [HttpPost]
    public async Task<IActionResult> DeletePortion(int portionId, int foodId)
    {
        var (succeeded, message) = await _adminRecipeService.DeletePortionAsync(portionId);
        if (succeeded)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Edit), new { id = foodId });
    }

    // Xac nhan xoa
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var food = await _context.Foods.FirstOrDefaultAsync(f => f.Id == id);
        if (food == null)
        {
            return NotFound();
        }

        var (canDelete, reason) = await _adminFoodService.CanDeleteFoodAsync(id);
        ViewBag.CanDelete = canDelete;
        ViewBag.DeleteReason = reason;

        return View(food);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var (succeeded, error) = await _adminFoodService.DeleteFoodAsync(id);
        if (!succeeded)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["SuccessMessage"] = "Xoa thuc pham thanh cong.";
        return RedirectToAction(nameof(Index));
    }
}
