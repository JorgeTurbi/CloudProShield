using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CloudShield.Migrations
{
    /// <inheritdoc />
    public partial class InicialCreacionTablas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SurName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dob = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Confirm = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "State",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.Id);
                    table.ForeignKey(
                        name: "FK_State_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpireTokenRequest = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenRefresh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpireTokenRefresh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Device = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRevoke = table.Column<bool>(type: "bit", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Line = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Address_State_StateId",
                        column: x => x.StateId,
                        principalTable: "State",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Address_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Country",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Afganistán" },
                    { 2, "Albania" },
                    { 3, "Algeria" },
                    { 4, "Samoa Americana" },
                    { 5, "Andorra" },
                    { 6, "Angola" },
                    { 7, "Anguilla" },
                    { 8, "Antártida" },
                    { 9, "Antigua y Barbuda" },
                    { 10, "Argentina" },
                    { 11, "Armenia" },
                    { 12, "Aruba" },
                    { 13, "Australia" },
                    { 14, "Austria" },
                    { 15, "Azerbaiyán" },
                    { 16, "Bahamas" },
                    { 17, "Bahrein" },
                    { 18, "Bangladesh" },
                    { 19, "Barbados" },
                    { 20, "Bielorrusia" },
                    { 21, "Bélgica" },
                    { 22, "Belice" },
                    { 23, "Benín" },
                    { 24, "Bermuda" },
                    { 25, "Bután" },
                    { 26, "Bolivia" },
                    { 27, "Bosnia-Herzegovina" },
                    { 28, "Botswana" },
                    { 29, "Brasil" },
                    { 30, "Brunei" },
                    { 31, "Bulgaria" },
                    { 32, "Burkina Faso" },
                    { 33, "Burundi" },
                    { 34, "Camboya" },
                    { 35, "Camerún" },
                    { 36, "Canadá" },
                    { 37, "Cabo Verde" },
                    { 38, "Islas Caimán" },
                    { 39, "República Centroafricana" },
                    { 40, "Chad" },
                    { 41, "Chile" },
                    { 42, "China" },
                    { 43, "Isla de Navidad" },
                    { 44, "Islas Cocos" },
                    { 45, "Colombia" },
                    { 46, "Comores" },
                    { 47, "República del Congo" },
                    { 48, "República Democrática del Congo" },
                    { 49, "Islas Cook" },
                    { 50, "Costa Rica" },
                    { 51, "Costa de Marfíl" },
                    { 52, "Croacia" },
                    { 53, "Cuba" },
                    { 54, "Chipre" },
                    { 55, "República Checa" },
                    { 56, "Dinamarca" },
                    { 57, "Djibouti" },
                    { 58, "Dominica" },
                    { 59, "República Dominicana" },
                    { 60, "Ecuador" },
                    { 61, "Egipto" },
                    { 62, "El Salvador" },
                    { 63, "Guinea Ecuatorial" },
                    { 64, "Eritrea" },
                    { 65, "Estonia" },
                    { 66, "Etiopía" },
                    { 67, "Islas Malvinas" },
                    { 68, "Islas Feroe" },
                    { 69, "Fiji" },
                    { 70, "Finlandia" },
                    { 71, "Francia" },
                    { 72, "Guyana Francesa" },
                    { 73, "Polinesia Francesa" },
                    { 74, "Tierras Australes y Antárticas Francesas" },
                    { 75, "Gabón" },
                    { 76, "Gambia" },
                    { 77, "Georgia" },
                    { 78, "Alemania" },
                    { 79, "Ghana" },
                    { 80, "Gibraltar" },
                    { 81, "Grecia" },
                    { 82, "Groenlandia" },
                    { 83, "Granada" },
                    { 84, "Guadalupe" },
                    { 85, "Guam" },
                    { 86, "Guatemala" },
                    { 87, "Guinea" },
                    { 88, "Guinea-Bissau" },
                    { 89, "Guyana" },
                    { 90, "Haití" },
                    { 91, "Vaticano" },
                    { 92, "Honduras" },
                    { 93, "Hong Kong" },
                    { 94, "Hungría" },
                    { 95, "Islandia" },
                    { 96, "India" },
                    { 97, "Indonesia" },
                    { 98, "Irán" },
                    { 99, "Iraq" },
                    { 100, "Irlanda" },
                    { 101, "Israel" },
                    { 102, "Italia" },
                    { 103, "Jamaica" },
                    { 104, "Japón" },
                    { 105, "Jordania" },
                    { 106, "Kazajstán" },
                    { 107, "Kenia" },
                    { 108, "Kiribati" },
                    { 109, "Corea del Norte" },
                    { 110, "Corea del Sur" },
                    { 111, "Kuwait" },
                    { 112, "Kirguistán" },
                    { 113, "Laos" },
                    { 114, "Letonia" },
                    { 115, "Líbano" },
                    { 116, "Lesotho" },
                    { 117, "Liberia" },
                    { 118, "Libia" },
                    { 119, "Liechtenstein" },
                    { 120, "Lituania" },
                    { 121, "Luxemburgo" },
                    { 122, "Macao" },
                    { 123, "Macedonia" },
                    { 124, "Madagascar" },
                    { 125, "Malawi" },
                    { 126, "Malasia" },
                    { 127, "Maldivas" },
                    { 128, "Mali" },
                    { 129, "Malta" },
                    { 130, "Islas Marshall" },
                    { 131, "Martinica" },
                    { 132, "Mauritania" },
                    { 133, "Mauricio" },
                    { 134, "Mayotte" },
                    { 135, "México" },
                    { 136, "Estados Federados de Micronesia" },
                    { 137, "Moldavia" },
                    { 138, "Mónaco" },
                    { 139, "Mongolia" },
                    { 140, "Montserrat" },
                    { 141, "Marruecos" },
                    { 142, "Mozambique" },
                    { 143, "Myanmar" },
                    { 144, "Namibia" },
                    { 145, "Nauru" },
                    { 146, "Nepal" },
                    { 147, "Holanda" },
                    { 148, "Antillas Holandesas" },
                    { 149, "Nueva Caledonia" },
                    { 150, "Nueva Zelanda" },
                    { 151, "Nicaragua" },
                    { 152, "Niger" },
                    { 153, "Nigeria" },
                    { 154, "Niue" },
                    { 155, "Islas Norfolk" },
                    { 156, "Islas Marianas del Norte" },
                    { 157, "Noruega" },
                    { 158, "Omán" },
                    { 159, "Pakistán" },
                    { 160, "Palau" },
                    { 161, "Palestina" },
                    { 162, "Panamá" },
                    { 163, "Papua Nueva Guinea" },
                    { 164, "Paraguay" },
                    { 165, "Perú" },
                    { 166, "Filipinas" },
                    { 167, "Pitcairn" },
                    { 168, "Polonia" },
                    { 169, "Portugal" },
                    { 170, "Puerto Rico" },
                    { 171, "Qatar" },
                    { 172, "Reunión" },
                    { 173, "Rumanía" },
                    { 174, "Rusia" },
                    { 175, "Ruanda" },
                    { 176, "Santa Helena" },
                    { 177, "San Kitts y Nevis" },
                    { 178, "Santa Lucía" },
                    { 179, "San Vicente y Granadinas" },
                    { 180, "Samoa" },
                    { 181, "San Marino" },
                    { 182, "Santo Tomé y Príncipe" },
                    { 183, "Arabia Saudita" },
                    { 184, "Senegal" },
                    { 185, "Serbia" },
                    { 186, "Seychelles" },
                    { 187, "Sierra Leona" },
                    { 188, "Singapur" },
                    { 189, "Eslovaquía" },
                    { 190, "Eslovenia" },
                    { 191, "Islas Salomón" },
                    { 192, "Somalia" },
                    { 193, "Sudáfrica" },
                    { 194, "España" },
                    { 195, "Sri Lanka" },
                    { 196, "Sudán" },
                    { 197, "Surinam" },
                    { 198, "Swazilandia" },
                    { 199, "Suecia" },
                    { 200, "Suiza" },
                    { 201, "Siria" },
                    { 202, "Taiwán" },
                    { 203, "Tadjikistan" },
                    { 204, "Tanzania" },
                    { 205, "Tailandia" },
                    { 206, "Timor Oriental" },
                    { 207, "Togo" },
                    { 208, "Tokelau" },
                    { 209, "Tonga" },
                    { 210, "Trinidad y Tobago" },
                    { 211, "Túnez" },
                    { 212, "Turquía" },
                    { 213, "Turkmenistan" },
                    { 214, "Islas Turcas y Caicos" },
                    { 215, "Tuvalu" },
                    { 216, "Uganda" },
                    { 217, "Ucrania" },
                    { 218, "Emiratos Árabes Unidos" },
                    { 219, "Reino Unido" },
                    { 220, "Estados Unidos" },
                    { 221, "Uruguay" },
                    { 222, "Uzbekistán" },
                    { 223, "Vanuatu" },
                    { 224, "Venezuela" },
                    { 225, "Vietnam" },
                    { 226, "Islas Vírgenes Británicas" },
                    { 227, "Islas Vírgenes Americanas" },
                    { 228, "Wallis y Futuna" },
                    { 229, "Sáhara Occidental" },
                    { 230, "Yemen" },
                    { 231, "Zambia" },
                    { 232, "Zimbabwe" }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "CreateAt", "Description", "Name", "UpdateAt" },
                values: new object[,]
                {
                    { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Write", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Reader", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 3, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "View", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 4, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Delete", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 5, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Update", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "Role",
                columns: new[] { "Id", "CreateAt", "Description", "Name", "UpdateAt" },
                values: new object[,]
                {
                    { 1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.", "Administrator", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality", "User", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Confirm", "ConfirmToken", "CreateAt", "Dob", "Email", "IsActive", "Name", "Password", "Phone", "SurName", "UpdateAt" },
                values: new object[] { 1, true, "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "jturbi@syschar.com", true, "Admin", "Lavacaloca@123", "8294627091", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

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

            migrationBuilder.CreateIndex(
                name: "IX_Address_CountryId",
                table: "Address",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_StateId",
                table: "Address",
                column: "StateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Address_UserId",
                table: "Address",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionsId",
                table: "RolePermissions",
                column: "PermissionsId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_UserId",
                table: "RolePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_State_CountryId",
                table: "State",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "State");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Country");
        }
    }
}
