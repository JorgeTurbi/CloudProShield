using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudShield.Migrations
{
    /// <inheritdoc />
    public partial class FixAddressStateRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Address_StateId",
                table: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateId",
                table: "Address",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Address_StateId",
                table: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateId",
                table: "Address",
                column: "StateId",
                unique: true);
        }
    }
}
