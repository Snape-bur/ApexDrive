using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexDrive.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeExtrasIntoBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChildSeatCost",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HasChildSeat",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceCost",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceType",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChildSeatCost",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "HasChildSeat",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "InsuranceCost",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "InsuranceType",
                table: "Bookings");
        }
    }
}
