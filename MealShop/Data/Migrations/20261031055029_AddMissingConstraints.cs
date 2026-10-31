using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Foods_DishId",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Portions_FoodId",
                table: "Portions");

            migrationBuilder.AddColumn<string>(
                name: "DishKind",
                table: "Recipes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "dish");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Foods_Id_Kind",
                table: "Foods",
                columns: new[] { "Id", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_DishId_DishKind",
                table: "Recipes",
                columns: new[] { "DishId", "DishKind" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Recipe_DishKind_Dish",
                table: "Recipes",
                sql: "DishKind = 'dish'");

            migrationBuilder.CreateIndex(
                name: "IX_Portions_FoodId_Name",
                table: "Portions",
                columns: new[] { "FoodId", "Name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Foods_Ingredient_UsableAsComponent",
                table: "Foods",
                sql: "Kind <> 'ingredient' OR UsableAsComponent = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Foods_Kind_Valid",
                table: "Foods",
                sql: "Kind IN ('ingredient', 'dish')");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Foods_DishId_DishKind",
                table: "Recipes",
                columns: new[] { "DishId", "DishKind" },
                principalTable: "Foods",
                principalColumns: new[] { "Id", "Kind" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recipes_Foods_DishId_DishKind",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_DishId_DishKind",
                table: "Recipes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Recipe_DishKind_Dish",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Portions_FoodId_Name",
                table: "Portions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Foods_Id_Kind",
                table: "Foods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Foods_Ingredient_UsableAsComponent",
                table: "Foods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Foods_Kind_Valid",
                table: "Foods");

            migrationBuilder.DropColumn(
                name: "DishKind",
                table: "Recipes");

            migrationBuilder.CreateIndex(
                name: "IX_Portions_FoodId",
                table: "Portions",
                column: "FoodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recipes_Foods_DishId",
                table: "Recipes",
                column: "DishId",
                principalTable: "Foods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
