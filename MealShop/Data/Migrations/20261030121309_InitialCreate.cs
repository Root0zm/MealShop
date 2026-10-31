using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsableAsComponent = table.Column<bool>(type: "bit", nullable: false),
                    IsSellable = table.Column<bool>(type: "bit", nullable: false),
                    NameVi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, collation: "Vietnamese_CI_AI"),
                    NameEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NameNorm = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CategoryVi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true, collation: "Vietnamese_CI_AI"),
                    Kcal = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Water = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Protein = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Fat = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Carb = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Fiber = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Cholesterol = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Calcium = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Phosphorus = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Iron = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Sodium = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Potassium = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    Zinc = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    VitaminA = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    BetaCarotene = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    VitaminC = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: true),
                    EnergyFlag = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    QaNote = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsSuspect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                    table.UniqueConstraint("AK_Foods_Id_UsableAsComponent", x => new { x.Id, x.UsableAsComponent });
                });

            migrationBuilder.CreateTable(
                name: "Portions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FoodId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Grams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portions", x => x.Id);
                    table.CheckConstraint("CK_Portion_Grams_Positive", "Grams > 0");
                    table.ForeignKey(
                        name: "FK_Portions_Foods_FoodId",
                        column: x => x.FoodId,
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    DishId = table.Column<int>(type: "int", nullable: false),
                    ComponentId = table.Column<int>(type: "int", nullable: false),
                    Grams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ComponentUsableAsComponent = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => new { x.DishId, x.ComponentId });
                    table.CheckConstraint("CK_Recipe_Component_Usable", "ComponentUsableAsComponent = 1");
                    table.CheckConstraint("CK_Recipe_Grams_Positive", "Grams > 0");
                    table.CheckConstraint("CK_Recipe_NoSelfReference", "DishId <> ComponentId");
                    table.ForeignKey(
                        name: "FK_Recipes_Foods_ComponentId_ComponentUsableAsComponent",
                        columns: x => new { x.ComponentId, x.ComponentUsableAsComponent },
                        principalTable: "Foods",
                        principalColumns: new[] { "Id", "UsableAsComponent" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recipes_Foods_DishId",
                        column: x => x.DishId,
                        principalTable: "Foods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Foods_CategoryVi",
                table: "Foods",
                column: "CategoryVi");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_Kind",
                table: "Foods",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_NameNorm",
                table: "Foods",
                column: "NameNorm");

            migrationBuilder.CreateIndex(
                name: "IX_Foods_NameVi",
                table: "Foods",
                column: "NameVi");

            migrationBuilder.CreateIndex(
                name: "IX_Portions_FoodId",
                table: "Portions",
                column: "FoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_ComponentId_ComponentUsableAsComponent",
                table: "Recipes",
                columns: new[] { "ComponentId", "ComponentUsableAsComponent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Portions");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "Foods");
        }
    }
}
