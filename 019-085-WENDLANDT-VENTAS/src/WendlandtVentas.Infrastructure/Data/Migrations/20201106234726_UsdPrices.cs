using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class UsdPrices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PriceUsd",
                table: "ProductPresentations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyType",
                table: "Orders",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceUsd",
                table: "ProductPresentations");

            migrationBuilder.DropColumn(
                name: "CurrencyType",
                table: "Orders");
        }
    }
}
