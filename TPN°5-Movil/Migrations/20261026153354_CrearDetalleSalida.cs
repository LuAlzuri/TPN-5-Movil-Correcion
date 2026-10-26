using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPN_5_MOVIL.Migrations
{
    /// <inheritdoc />
    public partial class CrearDetalleSalida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetallesSalida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalidaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DescAplicado = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesSalida", x => x.Id);
                    table.CheckConstraint("CK_DetalleSalida_Cantidad", "[Cantidad] >= 1");
                    table.CheckConstraint("CK_DetalleSalida_Descuento", "[DescAplicado] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_DetalleSalida_Precio", "[PrecioUnitario] >= 0");
                    table.ForeignKey(
                        name: "FK_DetallesSalida_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesSalida_Salidas_SalidaId",
                        column: x => x.SalidaId,
                        principalTable: "Salidas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesSalida_ProductoId",
                table: "DetallesSalida",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesSalida_SalidaId_ProductoId",
                table: "DetallesSalida",
                columns: new[] { "SalidaId", "ProductoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesSalida");
        }
    }
}
