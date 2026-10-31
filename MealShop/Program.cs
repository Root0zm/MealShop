using Microsoft.EntityFrameworkCore;
using MealShop.Data;

var builder = WebApplication.CreateBuilder(args);

// Dang ky AppDbContext voi SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Tu dong migrate va seed du lieu khi chay Development hoac co co --seed-only
if (app.Environment.IsDevelopment() || args.Contains("--seed-only"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await FoodSeeder.SeedAsync(context);
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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
