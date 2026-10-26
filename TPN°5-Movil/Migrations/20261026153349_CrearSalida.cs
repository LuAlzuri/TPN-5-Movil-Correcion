using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class CrearSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Salidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: true),
                    ConfirmarXCliente = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmarXEmpleado = table.Column<bool>(type: "bit", nullable: false),
                    FechaConfirmacionCliente = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaConfirmacionEmpleado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salidas", x => x.Id);
                    table.CheckConstraint("CK_Salida_Cliente", "([ConfirmarXCliente] = 0 AND [FechaConfirmacionCliente] IS NULL) OR ([ConfirmarXCliente] = 1 AND [FechaConfirmacionCliente] IS NOT NULL)");
                    table.CheckConstraint("CK_Salida_Empleado", "([ConfirmarXEmpleado] = 0 AND [EmpleadoId] IS NULL AND [FechaConfirmacionEmpleado] IS NULL) OR ([ConfirmarXEmpleado] = 1 AND [ConfirmarXCliente] = 1 AND [EmpleadoId] IS NOT NULL AND [FechaConfirmacionEmpleado] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Salidas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Salidas_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Salidas_ClienteId",
                table: "Salidas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Salidas_EmpleadoId",
                table: "Salidas",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Salidas_Fecha",
                table: "Salidas",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Salidas");
        }
    }
}
