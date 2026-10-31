using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortionPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Portions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Portions",
                type: "decimal(12,0)",
                precision: 12,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Portion_Price_NonNegative",
                table: "Portions",
                sql: "Price >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Portion_Price_NonNegative",
                table: "Portions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Portions");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Portions");
        }
    }
}
