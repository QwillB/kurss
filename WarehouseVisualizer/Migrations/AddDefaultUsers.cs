using Microsoft.EntityFrameworkCore.Migrations;
using System;
using WarehouseVisualizer.Models;
using WarehouseVisualizer.Services;

#nullable disable

namespace WarehouseVisualizer.Migrations
{
    public partial class AddDefaultUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var authService = new AuthService();

            // Администратор
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Username", "PasswordHash", "Role", "FullName", "IsActive", "CreatedAt" },
                values: new object[] {
                    "admin",
                    authService.HashPassword("123"),
                    (int)UserRole.Admin,
                    "Администратор системы",
                    true,
                    DateTime.Now
                });

            // Кладовщик
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Username", "PasswordHash", "Role", "FullName", "IsActive", "CreatedAt" },
                values: new object[] {
                    "storekeeper",
                    authService.HashPassword("store123"),
                    (int)UserRole.Storekeeper,
                    "Иванов Иван Иванович",
                    true,
                    DateTime.Now
                });

            // Аудитор
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Username", "PasswordHash", "Role", "FullName", "IsActive", "CreatedAt" },
                values: new object[] {
                    "auditor",
                    authService.HashPassword("audit123"),
                    (int)UserRole.Auditor,
                    "Петров Петр Петрович",
                    true,
                    DateTime.Now
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Username",
                keyValues: new object[] { "admin", "storekeeper", "auditor" });
        }
    }
}
