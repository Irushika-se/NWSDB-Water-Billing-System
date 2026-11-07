using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NWSDB.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    BillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.BillId);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillId = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaidByPartnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GatewayReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                });

            migrationBuilder.CreateTable(
                name: "WaterUsages",
                columns: table => new
                {
                    UsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitsConsumed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaterUsages", x => x.UsageId);
                });

            // Bills used to be seeded here with fixed IDs 1-3; they're now seeded by DbSeeder at
            // startup instead (see DemoSeedData.GetExtraBills), in one chronologically-ordered
            // list covering every demo customer, so Bill IDs line up with billing period.

            migrationBuilder.InsertData(
                table: "WaterUsages",
                columns: new[] { "UsageId", "BillingPeriod", "CustomerId", "ReadingDate", "UnitsConsumed" },
                values: new object[,]
                {
                    { 1, "2026-08", "C001", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 18.5m },
                    { 2, "2026-08", "C002", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 12.0m },
                    { 3, "2026-07", "C001", new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 16.2m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "WaterUsages");
        }
    }
}
