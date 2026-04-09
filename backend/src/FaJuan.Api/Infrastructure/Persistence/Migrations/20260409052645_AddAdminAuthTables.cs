using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaJuan.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuthTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminMenu",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Component = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sort = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminMenu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminRoleMenu",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminRoleId = table.Column<long>(type: "bigint", nullable: false),
                    AdminMenuId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRoleMenu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminUserRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<long>(type: "bigint", nullable: false),
                    AdminRoleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUserRole", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminRole_Code",
                table: "AdminRole",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminRoleMenu_AdminRoleId_AdminMenuId",
                table: "AdminRoleMenu",
                columns: new[] { "AdminRoleId", "AdminMenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUser_Username",
                table: "AdminUser",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserRole_AdminUserId_AdminRoleId",
                table: "AdminUserRole",
                columns: new[] { "AdminUserId", "AdminRoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminMenu");

            migrationBuilder.DropTable(
                name: "AdminRole");

            migrationBuilder.DropTable(
                name: "AdminRoleMenu");

            migrationBuilder.DropTable(
                name: "AdminUser");

            migrationBuilder.DropTable(
                name: "AdminUserRole");
        }
    }
}
