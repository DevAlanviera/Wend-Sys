using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class AgregarBanderaEnOrdenParaSaberSiSeDescontoInventario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InventoryDiscount",
                table: "Orders",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryDiscount",
                table: "Orders");
        }
    }
}
