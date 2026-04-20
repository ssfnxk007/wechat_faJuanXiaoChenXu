using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaJuan.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeChatPaySetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeChatPaySetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AppId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MerchantId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MerchantSerialNo = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PrivateKeyPem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiV3Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NotifyUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EnableMockFallback = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeChatPaySetting", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "WeChatPaySetting",
                columns: new[] { "Id", "ApiV3Key", "AppId", "EnableMockFallback", "MerchantId", "MerchantSerialNo", "NotifyUrl", "PrivateKeyPem", "UpdatedAt" },
                values: new object[] { 1, "", "", true, "", "", "", "", new DateTime(2026, 4, 20, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeChatPaySetting");
        }
    }
}
