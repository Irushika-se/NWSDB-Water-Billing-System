using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NWSDB.API.Migrations
{
    /// <inheritdoc />
    public partial class AddContactMessageStaffResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StaffResponse",
                table: "ContactMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StaffResponse",
                table: "ContactMessages");
        }
    }
}
