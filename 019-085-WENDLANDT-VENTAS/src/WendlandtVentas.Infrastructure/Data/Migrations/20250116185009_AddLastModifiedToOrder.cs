using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WendlandtVentas.Infrastructure.Data.Migrations
{
    public partial class AddLastModifiedToOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            /*migrationBuilder.CreateTable(
                name: "Bitacora",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Registro_id = table.Column<int>(nullable: false),
                    Usuario = table.Column<string>(maxLength: 255, nullable: false),
                    Fecha_modificacion = table.Column<DateTime>(nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Accion = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bitacora", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bitacora_Orders_Registro_id",
                        column: x => x.Registro_id,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });*/

            migrationBuilder.CreateIndex(
                name: "IX_Bitacora_Registro_id",
                table: "Bitacora",
                column: "Registro_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bitacora");

        }
    }
}
