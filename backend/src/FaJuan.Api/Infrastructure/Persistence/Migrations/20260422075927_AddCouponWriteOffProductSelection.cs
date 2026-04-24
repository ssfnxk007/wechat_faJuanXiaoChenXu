using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FaJuan.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponWriteOffProductSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProductId",
                table: "CouponWriteOffRecord",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "CouponWriteOffRecord");
        }
    }
}
