using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class CuilUnicoProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CUITNormalizado",
                table: "Proveedores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                computedColumnSql: "REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([CUIT], '-', ''), ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), '')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_CUITNormalizado",
                table: "Proveedores",
                column: "CUITNormalizado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_CUITNormalizado",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "CUITNormalizado",
                table: "Proveedores");
        }
    }
}
