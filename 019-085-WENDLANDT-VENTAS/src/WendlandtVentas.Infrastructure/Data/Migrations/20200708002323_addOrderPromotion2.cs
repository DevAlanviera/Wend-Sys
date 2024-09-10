using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class addOrderPromotion2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPromotionProducts",
                table: "OrderPromotionProducts");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "OrderPromotionProducts",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderPromotionProducts",
                table: "OrderPromotionProducts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPromotionProducts_OrderPromotionId",
                table: "OrderPromotionProducts",
                column: "OrderPromotionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPromotionProducts",
                table: "OrderPromotionProducts");

            migrationBuilder.DropIndex(
                name: "IX_OrderPromotionProducts_OrderPromotionId",
                table: "OrderPromotionProducts");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrderPromotionProducts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderPromotionProducts",
                table: "OrderPromotionProducts",
                columns: new[] { "OrderPromotionId", "ProductPresentationId" });
        }
    }
}
