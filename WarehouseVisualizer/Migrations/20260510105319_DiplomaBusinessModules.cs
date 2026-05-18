using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseVisualizer.Migrations
{
    /// <inheritdoc />
    public partial class DiplomaBusinessModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActionType",
                table: "OperationHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "OperationHistory",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FromLocation",
                table: "OperationHistory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaterialId",
                table: "OperationHistory",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "OperationHistory",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToLocation",
                table: "OperationHistory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "OperationHistory",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Materials",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Materials",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Materials",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationHistory_MaterialId",
                table: "OperationHistory",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_QrCode",
                table: "Materials",
                column: "QrCode");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type",
                table: "Notifications",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_OperationHistory_Materials_MaterialId",
                table: "OperationHistory",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationHistory_Materials_MaterialId",
                table: "OperationHistory");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_OperationHistory_MaterialId",
                table: "OperationHistory");

            migrationBuilder.DropIndex(
                name: "IX_Materials_QrCode",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "FromLocation",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "ToLocation",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "OperationHistory");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Materials");
        }
    }
}
