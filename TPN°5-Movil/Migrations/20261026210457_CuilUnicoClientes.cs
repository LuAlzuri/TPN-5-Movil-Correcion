using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class CuilUnicoClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CUITNormalizado",
                table: "Usuarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                computedColumnSql: "REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([CUIT], '-', ''), ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), '')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CUITNormalizado_Cliente",
                table: "Usuarios",
                column: "CUITNormalizado",
                unique: true,
                filter: "[Rol] = 'Cliente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CUITNormalizado_Cliente",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CUITNormalizado",
                table: "Usuarios");
        }
    }
}
