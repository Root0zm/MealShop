using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MealShop.Models;

namespace MealShop.Data;

public static class DemoSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var seedDemo = configuration.GetValue<bool>("Seed:Demo");
        if (!seedDemo)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        // 1. Tao tai khoan khach demo neu duoc cau hinh mat khau
        var demoCustomerPassword = configuration["Seed:DemoPassword"];
        ApplicationUser? demoCustomer = null;

        if (!string.IsNullOrWhiteSpace(demoCustomerPassword))
        {
            const string customerEmail = "demo.customer@mealshop.vn";
            demoCustomer = await userManager.FindByEmailAsync(customerEmail);
            if (demoCustomer == null)
            {
                demoCustomer = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FullName = "Khách Hàng Demo",
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(demoCustomer, demoCustomerPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(demoCustomer, "Customer");
                    logger.LogInformation("DemoSeeder: Da tao tai khoan khach demo ({Email}).", customerEmail);
                }
                else
                {
                    logger.LogWarning("DemoSeeder: Khong the tao khach demo: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
        else
        {
            logger.LogWarning("DemoSeeder: 'Seed:DemoPassword' chua duoc cau hinh trong User Secrets / AppSettings. Bo qua tao tai khoan khach demo.");
        }

        // 2. Tao tai khoan Admin demo neu Seed:DemoAdmin = true va co mat khau
        var seedDemoAdmin = configuration.GetValue<bool>("Seed:DemoAdmin");
        if (seedDemoAdmin)
        {
            var demoAdminPassword = configuration["Seed:DemoAdminPassword"];
            if (!string.IsNullOrWhiteSpace(demoAdminPassword))
            {
                const string adminEmail = "demo.admin@mealshop.vn";
                var demoAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (demoAdmin == null)
                {
                    demoAdmin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FullName = "Quản Trị Viên Demo",
                        EmailConfirmed = true
                    };
                    var createAdminResult = await userManager.CreateAsync(demoAdmin, demoAdminPassword);
                    if (createAdminResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(demoAdmin, "Admin");
                        logger.LogInformation("DemoSeeder: Da tao tai khoan Admin demo ({Email}).", adminEmail);
                    }
                    else
                    {
                        logger.LogWarning("DemoSeeder: Khong the tao Admin demo: {Errors}", string.Join(", ", createAdminResult.Errors.Select(e => e.Description)));
                    }
                }
            }
            else
            {
                logger.LogWarning("DemoSeeder: 'Seed:DemoAdminPassword' chua duoc cau hinh. Bo qua tao tai khoan Admin demo.");
            }
        }

        // 3. Tao toi da 5 don mau cho khach demo (Idempotent: kiem tra da co don demo chua)
        var existingDemoOrders = await context.Orders.CountAsync(o => o.CustomerName == "Khách demo");
        if (existingDemoOrders >= 5)
        {
            return;
        }

        // Lay mot so khau phan san sang ban
        var availablePortions = await context.Portions
            .Include(p => p.Food)
            .Where(p => p.IsActive && p.Food.IsSellable && !p.Food.IsSuspect)
            .Take(10)
            .ToListAsync();

        if (availablePortions.Count == 0)
        {
            logger.LogWarning("DemoSeeder: Chua co khau phan nao kha dung de tao don mau.");
            return;
        }

        var statuses = new[] { "Pending", "Confirmed", "Preparing", "Completed", "Completed" };
        var random = new Random(42); // Seed co dinh de idempotent

        int ordersToCreate = 5 - existingDemoOrders;
        for (int i = 0; i < ordersToCreate; i++)
        {
            var samplePortion = availablePortions[i % availablePortions.Count];
            var code = $"DEMO{i + 1:D2}K8X9M2"; // Ma 12 ky tu

            if (await context.Orders.AnyAsync(o => o.PublicCode == code))
            {
                continue;
            }

            var order = new Order
            {
                PublicCode = code,
                UserId = (i % 2 == 0 && demoCustomer != null) ? demoCustomer.Id : null,
                CustomerName = "Khách demo",
                PhoneNumber = "0987654321",
                ShippingAddress = "Địa chỉ demo, Tòa nhà MealShop, Hà Nội",
                Note = "Giao giờ trưa demo",
                Status = statuses[i % statuses.Length],
                TotalAmount = samplePortion.Price * 2,
                TotalKcal = samplePortion.Food.Kcal.HasValue ? samplePortion.Food.Kcal.Value * (samplePortion.Grams * 2 / 100m) : 500,
                CreatedAt = DateTime.UtcNow.AddHours(-i * 3)
            };

            order.Items.Add(new OrderItem
            {
                PortionId = samplePortion.Id,
                FoodId = samplePortion.FoodId,
                FoodName = samplePortion.Food.NameVi,
                PortionName = samplePortion.Name,
                PortionGrams = samplePortion.Grams,
                UnitPrice = samplePortion.Price,
                Quantity = 2,
                LinePrice = samplePortion.Price * 2,
                Kcal = samplePortion.Food.Kcal.HasValue ? samplePortion.Food.Kcal.Value * (samplePortion.Grams * 2 / 100m) : null,
                Protein = samplePortion.Food.Protein.HasValue ? samplePortion.Food.Protein.Value * (samplePortion.Grams * 2 / 100m) : null,
                Fat = samplePortion.Food.Fat.HasValue ? samplePortion.Food.Fat.Value * (samplePortion.Grams * 2 / 100m) : null,
                Carb = samplePortion.Food.Carb.HasValue ? samplePortion.Food.Carb.Value * (samplePortion.Grams * 2 / 100m) : null
            });

            context.Orders.Add(order);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("DemoSeeder: Da tao cac don hang demo mau.");
    }
}