 using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchdayApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthAndAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "PhoneNumber", "RoleId" },
                values: new object[] { 1, new DateTime(2026, 8, 28, 0, 0, 0, 0, DateTimeKind.Utc), "admin@matchday.local", "Quản trị viên", true, "$2b$11$QbGXPSCjgBrKb46nyvEQzOUNjKWaAD8oje32j6O6C6Nnq7P2OLxSW", null, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
