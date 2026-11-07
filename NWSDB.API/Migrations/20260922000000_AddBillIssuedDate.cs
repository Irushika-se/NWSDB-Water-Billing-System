using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NWSDB.API.Migrations
{
    /// <inheritdoc />
    public partial class AddBillIssuedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedDate",
                table: "Bills",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // The 3 fixed-ID demo bills these UpdateData calls used to backfill are no longer
            // seeded here (see InitialCreate + DemoSeedData) - all demo bills now get their
            // IssuedDate set directly at insert time, so there is nothing left to back-fill.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuedDate",
                table: "Bills");
        }
    }
}
