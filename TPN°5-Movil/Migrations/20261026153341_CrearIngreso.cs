using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class CrearIngreso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ingresos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Confirmado = table.Column<bool>(type: "bit", nullable: false),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpleadoConfirmacionId = table.Column<int>(type: "int", nullable: true),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingresos", x => x.Id);
                    table.CheckConstraint("CK_Ingreso_Confirmacion", "([Confirmado] = 0 AND [FechaConfirmacion] IS NULL AND [EmpleadoConfirmacionId] IS NULL) OR ([Confirmado] = 1 AND [FechaConfirmacion] IS NOT NULL AND [EmpleadoConfirmacionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Ingresos_Empleados_EmpleadoConfirmacionId",
                        column: x => x.EmpleadoConfirmacionId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ingresos_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ingresos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_EmpleadoConfirmacionId",
                table: "Ingresos",
                column: "EmpleadoConfirmacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_EmpleadoId",
                table: "Ingresos",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_Fecha",
                table: "Ingresos",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Ingresos_ProveedorId",
                table: "Ingresos",
                column: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ingresos");
        }
    }
}
