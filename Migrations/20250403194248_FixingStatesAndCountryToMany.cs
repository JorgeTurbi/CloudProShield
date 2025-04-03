using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudShield.Migrations
{
    /// <inheritdoc />
    public partial class FixingStatesAndCountryToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Address_CountryId",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_StateId",
                table: "Address");

            migrationBuilder.AddColumn<int>(
                name: "CountryId1",
                table: "Address",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateId1",
                table: "Address",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Address_CountryId",
                table: "Address",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CountryId1",
                table: "Address",
                column: "CountryId1",
                unique: true,
                filter: "[CountryId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateId",
                table: "Address",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateId1",
                table: "Address",
                column: "StateId1",
                unique: true,
                filter: "[StateId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_Country_CountryId1",
                table: "Address",
                column: "CountryId1",
                principalTable: "Country",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Address_State_StateId1",
                table: "Address",
                column: "StateId1",
                principalTable: "State",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Address_Country_CountryId1",
                table: "Address");

            migrationBuilder.DropForeignKey(
                name: "FK_Address_State_StateId1",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_CountryId",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_CountryId1",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_StateId",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_StateId1",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "CountryId1",
                table: "Address");

            migrationBuilder.DropColumn(
                name: "StateId1",
                table: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CountryId",
                table: "Address",
                column: "CountryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateId",
                table: "Address",
                column: "StateId",
                unique: true);
        }
    }
}
