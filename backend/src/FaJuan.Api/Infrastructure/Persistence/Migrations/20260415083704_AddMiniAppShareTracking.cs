using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaJuan.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMiniAppShareTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MiniAppShareEvent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ShareId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FromUserId = table.Column<long>(type: "bigint", nullable: true),
                    OpenUserId = table.Column<long>(type: "bigint", nullable: true),
                    VisitorKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TargetKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: true),
                    PagePath = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Scene = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    QueryJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClientTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiniAppShareEvent", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MiniAppShareEvent_EventKey",
                table: "MiniAppShareEvent",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MiniAppShareEvent_EventType_CreatedAt",
                table: "MiniAppShareEvent",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MiniAppShareEvent_FromUserId_CreatedAt",
                table: "MiniAppShareEvent",
                columns: new[] { "FromUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MiniAppShareEvent_OpenUserId_CreatedAt",
                table: "MiniAppShareEvent",
                columns: new[] { "OpenUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MiniAppShareEvent_ShareId",
                table: "MiniAppShareEvent",
                column: "ShareId");

            migrationBuilder.CreateIndex(
                name: "IX_MiniAppShareEvent_TargetType_TargetKey_CreatedAt",
                table: "MiniAppShareEvent",
                columns: new[] { "TargetType", "TargetKey", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MiniAppShareEvent");
        }
    }
}
