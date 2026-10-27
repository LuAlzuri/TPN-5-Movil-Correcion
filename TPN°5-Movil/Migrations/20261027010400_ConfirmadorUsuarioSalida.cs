using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmadorUsuarioSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salidas_Empleados_EmpleadoId",
                table: "Salidas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Salida_Empleado",
                table: "Salidas");

            migrationBuilder.RenameColumn(
                name: "EmpleadoId",
                table: "Salidas",
                newName: "UsuarioConfirmacionId");

            migrationBuilder.RenameIndex(
                name: "IX_Salidas_EmpleadoId",
                table: "Salidas",
                newName: "IX_Salidas_UsuarioConfirmacionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Salida_Empleado",
                table: "Salidas",
                sql: "([ConfirmarXEmpleado] = 0 AND [UsuarioConfirmacionId] IS NULL AND [FechaConfirmacionEmpleado] IS NULL) OR ([ConfirmarXEmpleado] = 1 AND [ConfirmarXCliente] = 1 AND [UsuarioConfirmacionId] IS NOT NULL AND [FechaConfirmacionEmpleado] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Salidas_Usuarios_UsuarioConfirmacionId",
                table: "Salidas",
                column: "UsuarioConfirmacionId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Salidas_Usuarios_UsuarioConfirmacionId",
                table: "Salidas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Salida_Empleado",
                table: "Salidas");

            migrationBuilder.RenameColumn(
                name: "UsuarioConfirmacionId",
                table: "Salidas",
                newName: "EmpleadoId");

            migrationBuilder.RenameIndex(
                name: "IX_Salidas_UsuarioConfirmacionId",
                table: "Salidas",
                newName: "IX_Salidas_EmpleadoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Salida_Empleado",
                table: "Salidas",
                sql: "([ConfirmarXEmpleado] = 0 AND [EmpleadoId] IS NULL AND [FechaConfirmacionEmpleado] IS NULL) OR ([ConfirmarXEmpleado] = 1 AND [ConfirmarXCliente] = 1 AND [EmpleadoId] IS NOT NULL AND [FechaConfirmacionEmpleado] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Salidas_Empleados_EmpleadoId",
                table: "Salidas",
                column: "EmpleadoId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
