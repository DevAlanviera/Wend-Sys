using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class addOrderPromotion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PresentationPromotions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OrderPromotions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false),
                    OrderId = table.Column<int>(nullable: false),
                    PromotionId = table.Column<int>(nullable: false),
                    Discount = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPromotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPromotions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderPromotions_Promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderPromotionProducts",
                columns: table => new
                {
                    OrderPromotionId = table.Column<int>(nullable: false),
                    ProductPresentationId = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPromotionProducts", x => new { x.OrderPromotionId, x.ProductPresentationId });
                    table.ForeignKey(
                        name: "FK_OrderPromotionProducts_OrderPromotions_OrderPromotionId",
                        column: x => x.OrderPromotionId,
                        principalTable: "OrderPromotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderPromotionProducts_ProductPresentations_ProductPresentationId",
                        column: x => x.ProductPresentationId,
                        principalTable: "ProductPresentations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPromotionProducts_ProductPresentationId",
                table: "OrderPromotionProducts",
                column: "ProductPresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPromotions_OrderId",
                table: "OrderPromotions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPromotions_PromotionId",
                table: "OrderPromotions",
                column: "PromotionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPromotionProducts");

            migrationBuilder.DropTable(
                name: "OrderPromotions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PresentationPromotions");
        }
    }
}
