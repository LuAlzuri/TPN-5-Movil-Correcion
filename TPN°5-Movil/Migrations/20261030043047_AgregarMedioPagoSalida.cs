using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMedioPagoSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MedioPago",
                table: "Salidas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Salida_MedioPago",
                table: "Salidas",
                sql: "[MedioPago] IS NULL OR [MedioPago] IN ('Tarjeta', 'Transferencia', 'Efectivo')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Salida_MedioPago",
                table: "Salidas");

            migrationBuilder.DropColumn(
                name: "MedioPago",
                table: "Salidas");
        }
    }
}
