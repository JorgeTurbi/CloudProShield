using CloudShield.Entities.Entity_Address;
using CloudShield.Entities.Role;
using Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace DataContext;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    //Todo: Entities Set TO Database
    public DbSet<User> User => Set<User>();
    public DbSet<Address> Address => Set<Address>();
    public DbSet<Country> Country => Set<Country>();
    public DbSet<State> State => Set<State>();
    public DbSet<Sessions> Sessions => Set<Sessions>();
    public DbSet<Role> Role => Set<Role>();
    public DbSet<Permissions> Permissions => Set<Permissions>();
    public DbSet<RolePermissions> RolePermissions => Set<RolePermissions>();


    //todo model created


    protected override void OnModelCreating(ModelBuilder model)
    {
        //todo Sessions to Users
        model.Entity<Sessions>().HasOne(a => a.User)
        .WithMany(a => a.Sessions)
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.NoAction);

        // todo Rolepermissions to Role
        model.Entity<RolePermissions>().HasOne(a => a.Role)
        .WithMany(a => a.RolePermissions)
        .HasForeignKey(a => a.RoleId)
        .OnDelete(DeleteBehavior.NoAction);

        // todo Rolepermissions to User
        model.Entity<RolePermissions>().HasOne(a => a.User)
        .WithMany(a => a.RolePermissions)
        .HasForeignKey(a => a.UserId)
        .OnDelete(DeleteBehavior.NoAction);

        // todo Rolepermissions to Permissions
        model.Entity<RolePermissions>().HasOne(a => a.Permissions)
        .WithMany(a => a.RolePermissions)
        .HasForeignKey(a => a.PermissionsId)
        .OnDelete(DeleteBehavior.NoAction);

        //todo User to Address
        model.Entity<User>()
        .HasOne(u => u.Address)
        .WithOne(a => a.User)
        .HasForeignKey<Address>(a => a.UserId)
        .OnDelete(DeleteBehavior.NoAction);

         //todo Address to Country
       model.Entity<Address>()
    .HasOne(a => a.Country)
    .WithMany(c => c.Address)
    .HasForeignKey(a => a.CountryId)
    .OnDelete(DeleteBehavior.NoAction);

            //todo Address to To State
        model.Entity<Address>()
        .HasOne(u => u.State)
        .WithOne(a => a.Address)
        .HasForeignKey<Address>(a => a.StateId)
        .OnDelete(DeleteBehavior.NoAction);

        //todo state to country 
        model.Entity<State>().HasOne(a => a.Country)
        .WithMany(a => a.State)
        .HasForeignKey(a => a.CountryId)
        .OnDelete(DeleteBehavior.NoAction);

        //todo data default Country
        model.Entity<Country>().HasData(
            new Country { Id = 1, Name = "Afganistán" },
            new Country { Id = 2, Name = "Albania" },
            new Country { Id = 3, Name = "Algeria" },
            new Country { Id = 4, Name = "Samoa Americana" },
            new Country { Id = 5, Name = "Andorra" },
            new Country { Id = 6, Name = "Angola" },
            new Country { Id = 7, Name = "Anguilla" },
            new Country { Id = 8, Name = "Antártida" },
            new Country { Id = 9, Name = "Antigua y Barbuda" },
            new Country { Id = 10, Name = "Argentina" },
            new Country { Id = 11, Name = "Armenia" },
            new Country { Id = 12, Name = "Aruba" },
            new Country { Id = 13, Name = "Australia" },
            new Country { Id = 14, Name = "Austria" },
            new Country { Id = 15, Name = "Azerbaiyán" },
            new Country { Id = 16, Name = "Bahamas" },
            new Country { Id = 17, Name = "Bahrein" },
            new Country { Id = 18, Name = "Bangladesh" },
            new Country { Id = 19, Name = "Barbados" },
            new Country { Id = 20, Name = "Bielorrusia" },
            new Country { Id = 21, Name = "Bélgica" },
            new Country { Id = 22, Name = "Belice" },
            new Country { Id = 23, Name = "Benín" },
            new Country { Id = 24, Name = "Bermuda" },
            new Country { Id = 25, Name = "Bután" },
            new Country { Id = 26, Name = "Bolivia" },
            new Country { Id = 27, Name = "Bosnia-Herzegovina" },
            new Country { Id = 28, Name = "Botswana" },
            new Country { Id = 29, Name = "Brasil" },
            new Country { Id = 30, Name = "Brunei" },
            new Country { Id = 31, Name = "Bulgaria" },
            new Country { Id = 32, Name = "Burkina Faso" },
            new Country { Id = 33, Name = "Burundi" },
            new Country { Id = 34, Name = "Camboya" },
            new Country { Id = 35, Name = "Camerún" },
            new Country { Id = 36, Name = "Canadá" },
            new Country { Id = 37, Name = "Cabo Verde" },
            new Country { Id = 38, Name = "Islas Caimán" },
            new Country { Id = 39, Name = "República Centroafricana" },
            new Country { Id = 40, Name = "Chad" },
            new Country { Id = 41, Name = "Chile" },
            new Country { Id = 42, Name = "China" },
            new Country { Id = 43, Name = "Isla de Navidad" },
            new Country { Id = 44, Name = "Islas Cocos" },
            new Country { Id = 45, Name = "Colombia" },
            new Country { Id = 46, Name = "Comores" },
            new Country { Id = 47, Name = "República del Congo" },
            new Country { Id = 48, Name = "República Democrática del Congo" },
            new Country { Id = 49, Name = "Islas Cook" },
            new Country { Id = 50, Name = "Costa Rica" },
            new Country { Id = 51, Name = "Costa de Marfíl" },
            new Country { Id = 52, Name = "Croacia" },
            new Country { Id = 53, Name = "Cuba" },
            new Country { Id = 54, Name = "Chipre" },
            new Country { Id = 55, Name = "República Checa" },
            new Country { Id = 56, Name = "Dinamarca" },
            new Country { Id = 57, Name = "Djibouti" },
            new Country { Id = 58, Name = "Dominica" },
            new Country { Id = 59, Name = "República Dominicana" },
            new Country { Id = 60, Name = "Ecuador" },
            new Country { Id = 61, Name = "Egipto" },
            new Country { Id = 62, Name = "El Salvador" },
            new Country { Id = 63, Name = "Guinea Ecuatorial" },
            new Country { Id = 64, Name = "Eritrea" },
            new Country { Id = 65, Name = "Estonia" },
            new Country { Id = 66, Name = "Etiopía" },
            new Country { Id = 67, Name = "Islas Malvinas" },
            new Country { Id = 68, Name = "Islas Feroe" },
            new Country { Id = 69, Name = "Fiji" },
            new Country { Id = 70, Name = "Finlandia" },
            new Country { Id = 71, Name = "Francia" },
            new Country { Id = 72, Name = "Guyana Francesa" },
            new Country { Id = 73, Name = "Polinesia Francesa" },
            new Country { Id = 74, Name = "Tierras Australes y Antárticas Francesas" },
            new Country { Id = 75, Name = "Gabón" },
            new Country { Id = 76, Name = "Gambia" },
            new Country { Id = 77, Name = "Georgia" },
            new Country { Id = 78, Name = "Alemania" },
            new Country { Id = 79, Name = "Ghana" },
            new Country { Id = 80, Name = "Gibraltar" },
            new Country { Id = 81, Name = "Grecia" },
            new Country { Id = 82, Name = "Groenlandia" },
            new Country { Id = 83, Name = "Granada" },
            new Country { Id = 84, Name = "Guadalupe" },
            new Country { Id = 85, Name = "Guam" },
            new Country { Id = 86, Name = "Guatemala" },
            new Country { Id = 87, Name = "Guinea" },
            new Country { Id = 88, Name = "Guinea-Bissau" },
            new Country { Id = 89, Name = "Guyana" },
            new Country { Id = 90, Name = "Haití" },
            new Country { Id = 91, Name = "Vaticano" },
            new Country { Id = 92, Name = "Honduras" },
            new Country { Id = 93, Name = "Hong Kong" },
            new Country { Id = 94, Name = "Hungría" },
            new Country { Id = 95, Name = "Islandia" },
            new Country { Id = 96, Name = "India" },
            new Country { Id = 97, Name = "Indonesia" },
            new Country { Id = 98, Name = "Irán" },
            new Country { Id = 99, Name = "Iraq" },
            new Country { Id = 100, Name = "Irlanda" },
            new Country { Id = 101, Name = "Israel" },
            new Country { Id = 102, Name = "Italia" },
            new Country { Id = 103, Name = "Jamaica" },
            new Country { Id = 104, Name = "Japón" },
            new Country { Id = 105, Name = "Jordania" },
            new Country { Id = 106, Name = "Kazajstán" },
            new Country { Id = 107, Name = "Kenia" },
            new Country { Id = 108, Name = "Kiribati" },
            new Country { Id = 109, Name = "Corea del Norte" },
            new Country { Id = 110, Name = "Corea del Sur" },
            new Country { Id = 111, Name = "Kuwait" },
            new Country { Id = 112, Name = "Kirguistán" },
            new Country { Id = 113, Name = "Laos" },
            new Country { Id = 114, Name = "Letonia" },
            new Country { Id = 115, Name = "Líbano" },
            new Country { Id = 116, Name = "Lesotho" },
            new Country { Id = 117, Name = "Liberia" },
            new Country { Id = 118, Name = "Libia" },
            new Country { Id = 119, Name = "Liechtenstein" },
            new Country { Id = 120, Name = "Lituania" },
            new Country { Id = 121, Name = "Luxemburgo" },
            new Country { Id = 122, Name = "Macao" },
            new Country { Id = 123, Name = "Macedonia" },
            new Country { Id = 124, Name = "Madagascar" },
            new Country { Id = 125, Name = "Malawi" },
            new Country { Id = 126, Name = "Malasia" },
            new Country { Id = 127, Name = "Maldivas" },
            new Country { Id = 128, Name = "Mali" },
            new Country { Id = 129, Name = "Malta" },
            new Country { Id = 130, Name = "Islas Marshall" },
            new Country { Id = 131, Name = "Martinica" },
            new Country { Id = 132, Name = "Mauritania" },
            new Country { Id = 133, Name = "Mauricio" },
            new Country { Id = 134, Name = "Mayotte" },
            new Country { Id = 135, Name = "México" },
            new Country { Id = 136, Name = "Estados Federados de Micronesia" },
            new Country { Id = 137, Name = "Moldavia" },
            new Country { Id = 138, Name = "Mónaco" },
            new Country { Id = 139, Name = "Mongolia" },
            new Country { Id = 140, Name = "Montserrat" },
            new Country { Id = 141, Name = "Marruecos" },
            new Country { Id = 142, Name = "Mozambique" },
            new Country { Id = 143, Name = "Myanmar" },
            new Country { Id = 144, Name = "Namibia" },
            new Country { Id = 145, Name = "Nauru" },
            new Country { Id = 146, Name = "Nepal" },
            new Country { Id = 147, Name = "Holanda" },
            new Country { Id = 148, Name = "Antillas Holandesas" },
            new Country { Id = 149, Name = "Nueva Caledonia" },
            new Country { Id = 150, Name = "Nueva Zelanda" },
            new Country { Id = 151, Name = "Nicaragua" },
            new Country { Id = 152, Name = "Niger" },
            new Country { Id = 153, Name = "Nigeria" },
            new Country { Id = 154, Name = "Niue" },
            new Country { Id = 155, Name = "Islas Norfolk" },
            new Country { Id = 156, Name = "Islas Marianas del Norte" },
            new Country { Id = 157, Name = "Noruega" },
            new Country { Id = 158, Name = "Omán" },
            new Country { Id = 159, Name = "Pakistán" },
            new Country { Id = 160, Name = "Palau" },
            new Country { Id = 161, Name = "Palestina" },
            new Country { Id = 162, Name = "Panamá" },
            new Country { Id = 163, Name = "Papua Nueva Guinea" },
            new Country { Id = 164, Name = "Paraguay" },
            new Country { Id = 165, Name = "Perú" },
            new Country { Id = 166, Name = "Filipinas" },
            new Country { Id = 167, Name = "Pitcairn" },
            new Country { Id = 168, Name = "Polonia" },
            new Country { Id = 169, Name = "Portugal" },
            new Country { Id = 170, Name = "Puerto Rico" },
            new Country { Id = 171, Name = "Qatar" },
            new Country { Id = 172, Name = "Reunión" },
            new Country { Id = 173, Name = "Rumanía" },
            new Country { Id = 174, Name = "Rusia" },
            new Country { Id = 175, Name = "Ruanda" },
            new Country { Id = 176, Name = "Santa Helena" },
            new Country { Id = 177, Name = "San Kitts y Nevis" },
            new Country { Id = 178, Name = "Santa Lucía" },
            new Country { Id = 179, Name = "San Vicente y Granadinas" },
            new Country { Id = 180, Name = "Samoa" },
            new Country { Id = 181, Name = "San Marino" },
            new Country { Id = 182, Name = "Santo Tomé y Príncipe" },
            new Country { Id = 183, Name = "Arabia Saudita" },
            new Country { Id = 184, Name = "Senegal" },
            new Country { Id = 185, Name = "Serbia" },
            new Country { Id = 186, Name = "Seychelles" },
            new Country { Id = 187, Name = "Sierra Leona" },
            new Country { Id = 188, Name = "Singapur" },
            new Country { Id = 189, Name = "Eslovaquía" },
            new Country { Id = 190, Name = "Eslovenia" },
            new Country { Id = 191, Name = "Islas Salomón" },
            new Country { Id = 192, Name = "Somalia" },
            new Country { Id = 193, Name = "Sudáfrica" },
            new Country { Id = 194, Name = "España" },
            new Country { Id = 195, Name = "Sri Lanka" },
            new Country { Id = 196, Name = "Sudán" },
            new Country { Id = 197, Name = "Surinam" },
            new Country { Id = 198, Name = "Swazilandia" },
            new Country { Id = 199, Name = "Suecia" },
            new Country { Id = 200, Name = "Suiza" },
            new Country { Id = 201, Name = "Siria" },
            new Country { Id = 202, Name = "Taiwán" },
            new Country { Id = 203, Name = "Tadjikistan" },
            new Country { Id = 204, Name = "Tanzania" },
            new Country { Id = 205, Name = "Tailandia" },
            new Country { Id = 206, Name = "Timor Oriental" },
            new Country { Id = 207, Name = "Togo" },
            new Country { Id = 208, Name = "Tokelau" },
            new Country { Id = 209, Name = "Tonga" },
            new Country { Id = 210, Name = "Trinidad y Tobago" },
            new Country { Id = 211, Name = "Túnez" },
            new Country { Id = 212, Name = "Turquía" },
            new Country { Id = 213, Name = "Turkmenistan" },
            new Country { Id = 214, Name = "Islas Turcas y Caicos" },
            new Country { Id = 215, Name = "Tuvalu" },
            new Country { Id = 216, Name = "Uganda" },
            new Country { Id = 217, Name = "Ucrania" },
            new Country { Id = 218, Name = "Emiratos Árabes Unidos" },
            new Country { Id = 219, Name = "Reino Unido" },
            new Country { Id = 220, Name = "Estados Unidos" },
            new Country { Id = 221, Name = "Uruguay" },
            new Country { Id = 222, Name = "Uzbekistán" },
            new Country { Id = 223, Name = "Vanuatu" },
            new Country { Id = 224, Name = "Venezuela" },
            new Country { Id = 225, Name = "Vietnam" },
            new Country { Id = 226, Name = "Islas Vírgenes Británicas" },
            new Country { Id = 227, Name = "Islas Vírgenes Americanas" },
            new Country { Id = 228, Name = "Wallis y Futuna" },
            new Country { Id = 229, Name = "Sáhara Occidental" },
            new Country { Id = 230, Name = "Yemen" },
            new Country { Id = 231, Name = "Zambia" },
            new Country { Id = 232, Name = "Zimbabwe" }
                );

 //todo data default State
  // todo state data default
    model.Entity<State>().HasData(
        new State { Id = 1, Name = "Alabama", CountryId = 220 },
        new State { Id = 2, Name = "Alaska", CountryId = 220 },
        new State { Id = 3, Name = "Arizona", CountryId = 220 },
        new State { Id = 4, Name = "Arkansas", CountryId = 220 },
        new State { Id = 5, Name = "California", CountryId = 220 },
        new State { Id = 6, Name = "Colorado", CountryId = 220 },
        new State { Id = 7, Name = "Connecticut", CountryId = 220 },
        new State { Id = 8, Name = "Delaware", CountryId = 220 },
        new State { Id = 9, Name = "Florida", CountryId = 220 },
        new State { Id = 10, Name = "Georgia", CountryId = 220 },
        new State { Id = 11, Name = "Hawaii", CountryId = 220 },
        new State { Id = 12, Name = "Idaho", CountryId = 220 },
        new State { Id = 13, Name = "Illinois", CountryId = 220 },
        new State { Id = 14, Name = "Indiana", CountryId = 220 },
        new State { Id = 15, Name = "Iowa", CountryId = 220 },
        new State { Id = 16, Name = "Kansas", CountryId = 220 },
        new State { Id = 17, Name = "Kentucky", CountryId = 220 },
        new State { Id = 18, Name = "Louisiana", CountryId = 220 },
        new State { Id = 19, Name = "Maine", CountryId = 220 },
        new State { Id = 20, Name = "Maryland", CountryId = 220 },
        new State { Id = 21, Name = "Massachusetts", CountryId = 220 },
        new State { Id = 22, Name = "Michigan", CountryId = 220 },
        new State { Id = 23, Name = "Minnesota", CountryId = 220 },
        new State { Id = 24, Name = "Mississippi", CountryId = 220 },
        new State { Id = 25, Name = "Missouri", CountryId = 220 },
        new State { Id = 26, Name = "Montana", CountryId = 220 },
        new State { Id = 27, Name = "Nebraska", CountryId = 220 },
        new State { Id = 28, Name = "Nevada", CountryId = 220 },
        new State { Id = 29, Name = "New Hampshire", CountryId = 220 },
        new State { Id = 30, Name = "New Jersey", CountryId = 220 },
        new State { Id = 31, Name = "New Mexico", CountryId = 220 },
        new State { Id = 32, Name = "New York", CountryId = 220 },
        new State { Id = 33, Name = "North Carolina", CountryId = 220 },
        new State { Id = 34, Name = "North Dakota", CountryId = 220 },
        new State { Id = 35, Name = "Ohio", CountryId = 220 },
        new State { Id = 36, Name = "Oklahoma", CountryId = 220 },
        new State { Id = 37, Name = "Oregon", CountryId = 220 },
        new State { Id = 38, Name = "Pennsylvania", CountryId = 220 },
        new State { Id = 39, Name = "Rhode Island", CountryId = 220 },
        new State { Id = 40, Name = "South Carolina", CountryId = 220 },
        new State { Id = 41, Name = "South Dakota", CountryId = 220 },
        new State { Id = 42, Name = "Tennessee", CountryId = 220 },
        new State { Id = 43, Name = "Texas", CountryId = 220 },
        new State { Id = 44, Name = "Utah", CountryId = 220 },
        new State { Id = 45, Name = "Vermont", CountryId = 220 },
        new State { Id = 46, Name = "Virginia", CountryId = 220 },
        new State { Id = 47, Name = "Washington", CountryId = 220 },
        new State { Id = 48, Name = "West Virginia", CountryId = 220 },
        new State { Id = 49, Name = "Wisconsin", CountryId = 220 },
        new State { Id = 50, Name = "Wyoming", CountryId = 220 },
        new State { Id = 51, Name = "District of Columbia", CountryId = 220 }
    );

        //todo permissions data default
        model.Entity<Permissions>().HasData(
            new Permissions
        {
            Id = 1,
            Name = "Write",

        }, new Permissions
        {
            Id = 2,
            Name = "Reader",

        }, new Permissions
        {
            Id = 3,
            Name = "View"
        }, new Permissions
        {
            Id = 4,
            Name = "Delete"
        }, new Permissions
        {
            Id = 5,
            Name = "Update"
        });

        //todo Role data default
       
       model.Entity<Role>().HasData(
        new Role{
        Id=1,
        Name="Administrator",
        Description="Has full access to all system features, settings, and user management. Responsible for maintaining and overseeing the platform.",
   
    
       }, new Role {
        Id=2,
        Name="User",
        Description="Has limited access to the system, can view and interact with allowed features based on their permissions. Typically focuses on using the core functionality",
      
       });
       
       //todo User Data Default
       model.Entity<User>().HasData(
        new User{
        Id=1,
        Name="Admin",
        Email="jturbi@syschar.com",
        Password="tyf/2baqRCXa00UpI2vvzoPLQVVqz4mDGbOrh3TT884ksq1zz1OxnDqg2ovromUd",
        Phone="8294627091",
        Confirm=true,
        IsActive=true,
        ConfirmToken=""
   
        
       });     
       
        base.OnModelCreating(model);

    }



}