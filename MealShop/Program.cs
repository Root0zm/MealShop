using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MealShop.Data;
using MealShop.Models;
using MealShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Dang ky AppDbContext voi SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cau hinh Options tu appsettings.json
builder.Services.Configure<MenuOptions>(builder.Configuration.GetSection("Menu"));
builder.Services.Configure<NutritionOptions>(builder.Configuration.GetSection("Nutrition"));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

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

// Dang ky Session va HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cau hinh Rate Limiting (gioi han tan suat chong spam theo IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "<div style='font-family:sans-serif;text-align:center;padding:50px;'>" +
            "<h2>Yêu cầu quá nhanh (429)</h2>" +
            "<p>Bạn đã gửi yêu cầu quá thường xuyên. Vui lòng chờ một phút rồi thử lại.</p>" +
            "<a href='/' style='color:#2F7D4F;text-decoration:none;font-weight:bold;'>← Quay về trang chủ</a>" +
            "</div>", token);
    };

    var rateLimitConfig = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();

    options.AddPolicy("track-limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfig.TrackLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("checkout-limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfig.CheckoutLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("login-limit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitConfig.LoginLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// Dang ky cac dich vu nghiep vu
builder.Services.AddScoped<NutritionCalculatorService>();
builder.Services.AddScoped<FoodQueryService>();
builder.Services.AddScoped<AdminFoodService>();
builder.Services.AddScoped<AdminRecipeService>();
builder.Services.AddScoped<DefaultPortionService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();

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

    if (args.Contains("--apply-defaults") || args.Contains("--seed-only"))
    {
        var defaultPortionService = scope.ServiceProvider.GetRequiredService<DefaultPortionService>();
        await defaultPortionService.ApplyAsync();
    }

    // Nạp dữ liệu demo nếu được cấu hình Seed:Demo = true
    await DemoSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
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

app.UseSession();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Endpoint kiem tra trang thai he thong (Health check)
app.MapGet("/health", async (AppDbContext context) =>
{
    var canConnect = await context.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Healthy" })
        : Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

// Route cho Areas (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Foods}/{action=Index}/{id?}");

// Route mac dinh
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Menu}/{action=Index}/{id?}");

app.Run();
