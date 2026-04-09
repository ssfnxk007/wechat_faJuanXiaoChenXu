using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaJuan.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MiniOpenId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UnionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OfficialOpenId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponOrder",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppUserId = table.Column<long>(type: "bigint", nullable: false),
                    CouponPackId = table.Column<long>(type: "bigint", nullable: false),
                    OrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    PaymentNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponPack",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SaleStartTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    SaleEndTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    PerUserLimit = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponPack", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponPackItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponPackId = table.Column<long>(type: "bigint", nullable: false),
                    CouponTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponPackItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemplateType = table.Column<int>(type: "int", nullable: false),
                    ValidPeriodType = table.Column<int>(type: "int", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThresholdAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValidDays = table.Column<int>(type: "int", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsNewUserOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsAllStores = table.Column<bool>(type: "bit", nullable: false),
                    PerUserLimit = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponWriteOffRecord",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserCouponId = table.Column<long>(type: "bigint", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WriteOffAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponWriteOffRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponOrderId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChannelTradeNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RawCallback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErpProductCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Store",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Store", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCoupon",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<long>(type: "bigint", nullable: false),
                    CouponTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    CouponOrderId = table.Column<long>(type: "bigint", nullable: true),
                    CouponCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCoupon", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_MiniOpenId",
                table: "AppUser",
                column: "MiniOpenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_Mobile",
                table: "AppUser",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_CouponOrder_OrderNo",
                table: "CouponOrder",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponPackItem_CouponPackId_CouponTemplateId",
                table: "CouponPackItem",
                columns: new[] { "CouponPackId", "CouponTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_PaymentNo",
                table: "PaymentTransaction",
                column: "PaymentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_ErpProductCode",
                table: "Product",
                column: "ErpProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Store_Code",
                table: "Store",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCoupon_CouponCode",
                table: "UserCoupon",
                column: "CouponCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "CouponOrder");

            migrationBuilder.DropTable(
                name: "CouponPack");

            migrationBuilder.DropTable(
                name: "CouponPackItem");

            migrationBuilder.DropTable(
                name: "CouponTemplate");

            migrationBuilder.DropTable(
                name: "CouponWriteOffRecord");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Store");

            migrationBuilder.DropTable(
                name: "UserCoupon");
        }
    }
}
