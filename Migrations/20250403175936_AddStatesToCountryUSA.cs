using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CloudShield.Migrations
{
    /// <inheritdoc />
    public partial class AddStatesToCountryUSA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "State",
                columns: new[] { "Id", "CountryId", "Name" },
                values: new object[,]
                {
                    { 1, 220, "Alabama" },
                    { 2, 220, "Alaska" },
                    { 3, 220, "Arizona" },
                    { 4, 220, "Arkansas" },
                    { 5, 220, "California" },
                    { 6, 220, "Colorado" },
                    { 7, 220, "Connecticut" },
                    { 8, 220, "Delaware" },
                    { 9, 220, "Florida" },
                    { 10, 220, "Georgia" },
                    { 11, 220, "Hawaii" },
                    { 12, 220, "Idaho" },
                    { 13, 220, "Illinois" },
                    { 14, 220, "Indiana" },
                    { 15, 220, "Iowa" },
                    { 16, 220, "Kansas" },
                    { 17, 220, "Kentucky" },
                    { 18, 220, "Louisiana" },
                    { 19, 220, "Maine" },
                    { 20, 220, "Maryland" },
                    { 21, 220, "Massachusetts" },
                    { 22, 220, "Michigan" },
                    { 23, 220, "Minnesota" },
                    { 24, 220, "Mississippi" },
                    { 25, 220, "Missouri" },
                    { 26, 220, "Montana" },
                    { 27, 220, "Nebraska" },
                    { 28, 220, "Nevada" },
                    { 29, 220, "New Hampshire" },
                    { 30, 220, "New Jersey" },
                    { 31, 220, "New Mexico" },
                    { 32, 220, "New York" },
                    { 33, 220, "North Carolina" },
                    { 34, 220, "North Dakota" },
                    { 35, 220, "Ohio" },
                    { 36, 220, "Oklahoma" },
                    { 37, 220, "Oregon" },
                    { 38, 220, "Pennsylvania" },
                    { 39, 220, "Rhode Island" },
                    { 40, 220, "South Carolina" },
                    { 41, 220, "South Dakota" },
                    { 42, 220, "Tennessee" },
                    { 43, 220, "Texas" },
                    { 44, 220, "Utah" },
                    { 45, 220, "Vermont" },
                    { 46, 220, "Virginia" },
                    { 47, 220, "Washington" },
                    { 48, 220, "West Virginia" },
                    { 49, 220, "Wisconsin" },
                    { 50, 220, "Wyoming" },
                    { 51, 220, "District of Columbia" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "State",
                keyColumn: "Id",
                keyValue: 51);
        }
    }
}
