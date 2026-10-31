using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MealShop.Models;

namespace MealShop.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Food> Foods => Set<Food>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Portion> Portions => Set<Portion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cau hinh thong tin bo sung cho ApplicationUser
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        });

        // Cau hinh bang Foods
        modelBuilder.Entity<Food>(entity =>
        {
            entity.ToTable("Foods", t =>
            {
                // Kind chi duoc phep la ingredient hoac dish
                t.HasCheckConstraint("CK_Foods_Kind_Valid", "Kind IN ('ingredient', 'dish')");
                // Nguyen lieu bat buoc phai co UsableAsComponent = 1
                t.HasCheckConstraint("CK_Foods_Ingredient_UsableAsComponent", "Kind <> 'ingredient' OR UsableAsComponent = 1");
            });

            // Khoa chinh khong tu tang vi dung ma goc tu dataset
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Id).ValueGeneratedNever();

            // Khoa phu de ho tro khoa ngoai ghep voi Recipe
            entity.HasAlternateKey(f => new { f.Id, f.UsableAsComponent });
            entity.HasAlternateKey(f => new { f.Id, f.Kind });

            // Do dai chuoi va rang buoc
            entity.Property(f => f.Kind).HasMaxLength(30).IsRequired();
            entity.Property(f => f.NameVi).HasMaxLength(255).IsRequired();
            entity.Property(f => f.NameEn).HasMaxLength(255);
            entity.Property(f => f.NameNorm).HasMaxLength(255);
            entity.Property(f => f.CategoryVi).HasMaxLength(150);
            entity.Property(f => f.EnergyFlag).HasMaxLength(10);
            entity.Property(f => f.QaNote).HasMaxLength(255);

            // Collation tieng Viet khi chay tren SQL Server
            if (Database.IsSqlServer())
            {
                entity.Property(f => f.NameVi).UseCollation("Vietnamese_CI_AI");
                entity.Property(f => f.CategoryVi).UseCollation("Vietnamese_CI_AI");
            }

            // Chi muc ho tro tim kiem
            entity.HasIndex(f => f.NameVi);
            entity.HasIndex(f => f.NameNorm);
            entity.HasIndex(f => f.CategoryVi);
            entity.HasIndex(f => f.Kind);

            // Dinh dang so thap phan cho chi so dinh duong
            var decimalProps = new[]
            {
                nameof(Food.Kcal), nameof(Food.Water), nameof(Food.Protein), nameof(Food.Fat),
                nameof(Food.Carb), nameof(Food.Fiber), nameof(Food.Cholesterol), nameof(Food.Calcium),
                nameof(Food.Phosphorus), nameof(Food.Iron), nameof(Food.Sodium), nameof(Food.Potassium),
                nameof(Food.Zinc), nameof(Food.VitaminA), nameof(Food.BetaCarotene), nameof(Food.VitaminC)
            };

            foreach (var prop in decimalProps)
            {
                entity.Property(prop).HasPrecision(12, 3);
            }
        });

        // Cau hinh bang Recipes
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Recipes", t =>
            {
                // Rang buoc mon khong duoc tu tham chieu chinh no
                t.HasCheckConstraint("CK_Recipe_NoSelfReference", "DishId <> ComponentId");
                // Khoi luong gram phai lon hon 0
                t.HasCheckConstraint("CK_Recipe_Grams_Positive", "Grams > 0");
                // Thanh phan phai co co UsableAsComponent bang true (1)
                t.HasCheckConstraint("CK_Recipe_Component_Usable", "ComponentUsableAsComponent = 1");
                // Mon an trong cong thuc phai co DishKind = 'dish'
                t.HasCheckConstraint("CK_Recipe_DishKind_Dish", "DishKind = 'dish'");
            });

            // Khoa chinh ghep gom DishId va ComponentId
            entity.HasKey(r => new { r.DishId, r.ComponentId });

            entity.Property(r => r.Grams).HasPrecision(10, 2).IsRequired();
            entity.Property(r => r.DishKind).HasMaxLength(30).HasDefaultValue("dish");
            entity.Property(r => r.ComponentUsableAsComponent).HasDefaultValue(true);

            // Quan he voi mon an goc qua khoa ngoai ghep (DishId, DishKind)
            entity.HasOne(r => r.Dish)
                .WithMany(f => f.Recipes)
                .HasForeignKey(r => new { r.DishId, r.DishKind })
                .HasPrincipalKey(f => new { f.Id, f.Kind })
                .OnDelete(DeleteBehavior.Cascade);

            // Quan he voi thanh phan thong qua khoa ngoai ghep
            entity.HasOne(r => r.Component)
                .WithMany(f => f.UsedInRecipes)
                .HasForeignKey(r => new { r.ComponentId, r.ComponentUsableAsComponent })
                .HasPrincipalKey(f => new { f.Id, f.UsableAsComponent })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Cau hinh bang Portions
        modelBuilder.Entity<Portion>(entity =>
        {
            entity.ToTable("Portions", t =>
            {
                t.HasCheckConstraint("CK_Portion_Grams_Positive", "Grams > 0");
                t.HasCheckConstraint("CK_Portion_Price_NonNegative", "Price >= 0");
            });

            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Grams).HasPrecision(10, 2).IsRequired();
            entity.Property(p => p.Price).HasPrecision(12, 0).HasDefaultValue(0m);
            entity.Property(p => p.IsActive).HasDefaultValue(true);

            // Khau phan trong cung mot mon khong duoc trung ten
            entity.HasIndex(p => new { p.FoodId, p.Name }).IsUnique();

            entity.HasOne(p => p.Food)
                .WithMany(f => f.Portions)
                .HasForeignKey(p => p.FoodId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateEntities()
    {
        var foodEntries = ChangeTracker.Entries<Food>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in foodEntries)
        {
            var food = entry.Entity;
            if (food.Kind != "ingredient" && food.Kind != "dish")
            {
                throw new InvalidOperationException("Kind chi duoc phep la 'ingredient' hoac 'dish'.");
            }

            if (food.Kind == "ingredient" && !food.UsableAsComponent)
            {
                throw new InvalidOperationException("Nguyen lieu bat buoc phai co UsableAsComponent = true.");
            }
        }

        var recipeEntries = ChangeTracker.Entries<Recipe>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in recipeEntries)
        {
            var recipe = entry.Entity;

            if (recipe.DishId == recipe.ComponentId)
            {
                throw new InvalidOperationException("Mon khong duoc la thanh phan cua chinh no.");
            }

            if (recipe.Grams <= 0)
            {
                throw new ArgumentException("Khoi luong thanh phan phai lon hon 0 gram.");
            }

            if (recipe.Dish != null)
            {
                if (recipe.Dish.Kind != "dish")
                {
                    throw new InvalidOperationException("Mon an trong cong thuc phai co Kind la 'dish'.");
                }
                recipe.DishKind = recipe.Dish.Kind;
            }

            if (recipe.DishKind != "dish")
            {
                throw new InvalidOperationException("Mon an trong cong thuc phai co Kind la 'dish'.");
            }

            if (recipe.Component != null)
            {
                if (!recipe.Component.UsableAsComponent)
                {
                    throw new InvalidOperationException("Thanh phan phai co UsableAsComponent = true.");
                }
                recipe.ComponentUsableAsComponent = recipe.Component.UsableAsComponent;
            }
        }

        var portionEntries = ChangeTracker.Entries<Portion>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in portionEntries)
        {
            var portion = entry.Entity;
            if (portion.Grams <= 0)
            {
                throw new ArgumentException("Khoi luong khau phan phai lon hon 0 gram.");
            }
            if (portion.Price < 0)
            {
                throw new ArgumentException("Gia ban khau phan khong duoc am.");
            }
        }
    }
}
