using Microsoft.AspNetCore.Mvc;

namespace MealShop.ViewComponents;

public class MiniCartViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        // O Phan C, MiniCart hien thi trang thai rong; se noi du lieu Session that o Phan D
        return View();
    }
}
