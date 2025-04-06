# Notas de Control 02/04/2025

#  Queda Pendiente Implementar Confirmarcion por Correo y actualizar estado del Usuario.

#  Revisar la implmentacion de Logs

# 📘 CloudShield - Proyecto en .NET Core 9

Este proyecto utiliza una arquitectura en capas implementada con .NET Core 9, incluyendo configuración de logging, seguridad de contraseñas, y manejo de errores comunes en base de datos.

---

### 🛠️ Configuración del Proyecto

Framework utilizado: `.NET Core 9`

Arquitectura basada en capas:
- Controladores
- Servicios
- Repositorios

---

### 📦 Paquetes Instalados

```bash
dotnet add package Serilog
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

---

### 📄 Configuración de Logs con Serilog

#### En `Program.cs`:

```csharp
var logFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "LogsApplication");

if (!Directory.Exists(logFolderPath))
{
    Directory.CreateDirectory(logFolderPath);
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(logFolderPath, "logApplication-.txt"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

#### En `appsettings.json`:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "File",
      "Args": {
        "path": "LogsApplication/logApplication-.txt",
        "rollingInterval": "Day"
      }
    }
  ],
  "Enrich": [ "FromLogContext" ]
}
```

---

### 🔁 Ejecución y Modo Debug

#### Compilar y ejecutar en modo Debug:

```bash
dotnet build --configuration Debug
dotnet run --configuration Debug
```

#### Modo Hot Reload:

```bash
dotnet watch run
```

> Esto mantiene la app corriendo y recarga automáticamente al detectar cambios.

---

### ⚙️ Configuración del archivo `launchSettings.json`

```json
{
  "profiles": {
    "CloudShield": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7200;http://localhost:5030",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

### 🔐 Cifrado y Validación de Contraseña

#### Clase `PasswordHasher.cs`:

```csharp
public static class PasswordHasher
{
    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        byte[] hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        byte[] hashBytes = Convert.FromBase64String(hashedPassword);
        byte[] salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);

        for (int i = 0; i < 32; i++)
        {
            if (hashBytes[i + 16] != hash[i])
                return false;
        }

        return true;
    }
}
```

---

### ⚠️ Manejo de error: Clave duplicada en índice único

#### Error:

`Cannot insert duplicate key row in object 'dbo.Address' with unique index 'IX_Address_CountryId'`

#### Causa:

Estaba definida una relación uno-a-uno entre `Address` y `Country`, creando un índice único en `CountryId`.

#### Solución aplicada:

Se cambió a una relación uno-a-muchos:

```csharp
model.Entity<Address>()
    .HasOne(a => a.Country)
    .WithMany(c => c.Addresses)
    .HasForeignKey(a => a.CountryId)
    .OnDelete(DeleteBehavior.NoAction);
```

---

