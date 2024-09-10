using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class addNamePromotion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Promotion",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Promotion");
        }
    }
}
