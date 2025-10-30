using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingPaymentRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentId",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentId",
                table: "Bookings",
                type: "int",
                nullable: true);
        }
    }
}
