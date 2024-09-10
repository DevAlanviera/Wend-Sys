using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class AnadirBanderaDeObsequioEnProductoEnOrden : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPresent",
                table: "OrderProducts",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPresent",
                table: "OrderProducts");
        }
    }
}
