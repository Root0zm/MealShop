using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Dang ky AppDbContext voi SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cau hinh MenuOptions tu appsettings.json
builder.Services.Configure<MenuOptions>(builder.Configuration.GetSection("Menu"));

// Cau hinh ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Mat khau toi thieu 8 ky tu, co chu va so
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Email duy nhat
    options.User.RequireUniqueEmail = true;

    // Khoa tai khoan neu dang nhap sai 5 lan lien tiep trong 10 phut
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cau hinh Cookie xac thuc
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Dang ky cac dich vu nghiep vu
builder.Services.AddScoped<NutritionCalculatorService>();
builder.Services.AddScoped<FoodQueryService>();
builder.Services.AddScoped<AdminFoodService>();
builder.Services.AddScoped<AdminRecipeService>();
builder.Services.AddScoped<DefaultPortionService>();
builder.Services.AddScoped<MenuService>();

// Add services to the container voi tu dong kiem tra Antiforgery Token cho POST
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Tu dong migrate va seed du lieu khi chay Development hoac co co --seed-only
if (app.Environment.IsDevelopment() || args.Contains("--seed-only"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await FoodSeeder.SeedAsync(context);
    await FoodSeeder.SeedRecipesAsync(context);
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// Ho tro chay lenh seed rieng biet ma khong khoi dong web server
if (args.Contains("--seed-only"))
{
    Console.WriteLine("Seed completed successfully.");
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Route cho Areas (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Foods}/{action=Index}/{id?}");

// Route mac dinh
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Menu}/{action=Index}/{id?}");

app.Run();
