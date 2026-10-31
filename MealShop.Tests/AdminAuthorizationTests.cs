using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealShop.Tests;

public class AdminAuthorizationTests
{
    [Fact]
    public void AllAdminAreaControllers_MustHaveAuthorizeAdminRole()
    {
        // Lay tat ca cac Controller trong assembly MealShop thuoc Area Admin
        var assembly = typeof(MealShop.Areas.Admin.Controllers.FoodsController).Assembly;

        var adminControllers = assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) || typeof(ControllerBase).IsAssignableFrom(t))
            .Where(t =>
            {
                var areaAttr = t.GetCustomAttribute<AreaAttribute>();
                return areaAttr != null && areaAttr.RouteValue == "Admin";
            })
            .ToList();

        Assert.NotEmpty(adminControllers);

        foreach (var controller in adminControllers)
        {
            var authAttr = controller.GetCustomAttribute<AuthorizeAttribute>();
            Assert.True(
                authAttr != null,
                $"Controller {controller.Name} trong Area Admin thieu thuoc tinh [Authorize].");

            Assert.True(
                authAttr.Roles != null && authAttr.Roles.Split(',').Select(r => r.Trim()).Contains("Admin"),
                $"Controller {controller.Name} trong Area Admin khong yeu cau Role 'Admin'.");
        }
    }
}
