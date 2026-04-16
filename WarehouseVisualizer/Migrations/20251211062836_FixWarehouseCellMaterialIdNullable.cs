using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseVisualizer.Migrations
{
    /// <inheritdoc />
    public partial class FixWarehouseCellMaterialIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseCells_Materials_MaterialId",
                table: "WarehouseCells");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseCells_Materials_MaterialId",
                table: "WarehouseCells",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseCells_Materials_MaterialId",
                table: "WarehouseCells");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseCells_Materials_MaterialId",
                table: "WarehouseCells",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id");
        }
    }
}
