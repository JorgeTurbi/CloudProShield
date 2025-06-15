using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudShield.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d3f9d6c9-b4f5-4e5b-a6f1-cab3fbe287a7"),
                column: "Password",
                value: "WJd8pmbNUhSKz3M0V/KT2mTO0KlCiGkTPOLdARs0WIfOKfZjObvThYTnW/WmPnXc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d3f9d6c9-b4f5-4e5b-a6f1-cab3fbe287a7"),
                column: "Password",
                value: "tyf/2baqRCXa00UpI2vvzoPLQVVqz4mDGbOrh3TT884ksq1zz1OxnDqg2ovromUd");
        }
    }
}
