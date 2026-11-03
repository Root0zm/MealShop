using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MealShop.Data;
using MealShop.Models;
using Xunit;

namespace MealShop.Tests;

public class DemoSeederAndHealthTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public DemoSeederAndHealthTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task Database_HealthCheck_ShouldReturnTrueWhenConnected()
    {
        using var context = new AppDbContext(_options);
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task DemoSeeder_ShouldDoNothingWhenSeedDemoIsFalse()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(_connection));
        services.AddLogging();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:Demo"] = "false"
            })
            .Build();

        var provider = services.BuildServiceProvider();

        await DemoSeeder.SeedAsync(provider, configuration);

        using var context = new AppDbContext(_options);
        var ordersCount = await context.Orders.CountAsync();
        Assert.Equal(0, ordersCount);
    }
}