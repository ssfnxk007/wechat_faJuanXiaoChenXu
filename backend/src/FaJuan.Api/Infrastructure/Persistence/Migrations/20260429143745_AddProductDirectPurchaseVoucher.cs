using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaJuan.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDirectPurchaseVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemProductVoucher",
                table: "CouponTemplate",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long?>(
                name: "ProductId",
                table: "CouponOrder",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "CouponOrder",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductErpProductCodeSnapshot",
                table: "CouponOrder",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "CouponOrder",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<long?>(
                name: "DirectPurchaseCouponTemplateId",
                table: "Product",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "DirectPurchaseValidDays",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectPurchaseValidFrom",
                table: "Product",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int?>(
                name: "DirectPurchaseValidPeriodType",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DirectPurchaseValidTo",
                table: "Product",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoundErpProductCode",
                table: "UserCoupon",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long?>(
                name: "BoundProductId",
                table: "UserCoupon",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoundProductName",
                table: "UserCoupon",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "UserCoupon",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemProductVoucher",
                table: "CouponTemplate");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CouponOrder");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "CouponOrder");

            migrationBuilder.DropColumn(
                name: "ProductErpProductCodeSnapshot",
                table: "CouponOrder");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "CouponOrder");

            migrationBuilder.DropColumn(
                name: "DirectPurchaseCouponTemplateId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DirectPurchaseValidDays",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DirectPurchaseValidFrom",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DirectPurchaseValidPeriodType",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "DirectPurchaseValidTo",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "BoundErpProductCode",
                table: "UserCoupon");

            migrationBuilder.DropColumn(
                name: "BoundProductId",
                table: "UserCoupon");

            migrationBuilder.DropColumn(
                name: "BoundProductName",
                table: "UserCoupon");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "UserCoupon");
        }
    }
}
