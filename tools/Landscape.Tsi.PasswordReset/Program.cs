using System.Text.Json;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var actorTechnical = Environment.GetEnvironmentVariable("LANDSCAPE_TSI_PASSWORD_RESET_ACTOR");
var databasePolicy = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
{
    ["Development"] = "db-landscape-tsi-dev",
    ["Staging"] = "db-landscape-tsi-dev",
    ["Production"] = null
};

if (!databasePolicy.TryGetValue(environmentName, out var allowedDatabase) || allowedDatabase is null)
{
    Console.Error.WriteLine($"Operación rechazada: no existe una base autorizada para Environment={environmentName}.");
    return 2;
}

if (string.IsNullOrWhiteSpace(actorTechnical))
{
    Console.Error.WriteLine("Operación rechazada: configure LANDSCAPE_TSI_PASSWORD_RESET_ACTOR.");
    return 3;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("src/Landscape.Tsi.Web/appsettings.json", optional: false)
    .AddJsonFile($"src/Landscape.Tsi.Web/appsettings.{environmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<ProgramMarker>(optional: true);
builder.Logging.ClearProviders();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var connectionString = builder.Configuration.GetConnectionString("LandscapeTsiDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Operación rechazada: ConnectionStrings:LandscapeTsiDb no está configurada.");
    return 4;
}

SqlConnectionStringBuilder connection;
try
{
    connection = new SqlConnectionStringBuilder(connectionString);
}
catch
{
    Console.Error.WriteLine("Operación rechazada: la conexión configurada no es válida.");
    return 5;
}

if (!string.Equals(connection.InitialCatalog, allowedDatabase, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Operación rechazada: la base configurada no coincide con la política del ambiente.");
    return 6;
}

Console.WriteLine($"Environment: {environmentName}");
Console.WriteLine($"SQL Server: {connection.DataSource}");
Console.WriteLine($"Database: {connection.InitialCatalog}");

Console.Write("Usuario local a restablecer: ");
var targetUserName = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(targetUserName))
{
    Console.Error.WriteLine("Operación cancelada: debe indicar un usuario local.");
    return 7;
}

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<IamUsuario>>();
var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
var user = await manager.FindByNameAsync(targetUserName);
if (user is null)
{
    Console.Error.WriteLine("El usuario local indicado no existe.");
    return 8;
}

Console.WriteLine($"User: {user.UserName}");

if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
{
    var confirmationText = $"RESET {user.UserName}";
    Console.Write($"Escriba exactamente {confirmationText} para continuar: ");
    if (!string.Equals(Console.ReadLine(), confirmationText, StringComparison.Ordinal))
    {
        Console.Error.WriteLine("Operación cancelada: confirmación incorrecta.");
        return 7;
    }
}

Console.Write("Nueva contraseña: ");
var password = ReadSecret();
Console.Write("Confirmar contraseña: ");
var confirmation = ReadSecret();
Console.WriteLine();
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Las contraseñas no coinciden.");
    return 8;
}

try
{
    var result = await PasswordResetWorkflow.ResetAndUnlockAsync(manager, user, password);
    if (!result.Succeeded)
    {
        Console.Error.WriteLine("No se pudo restablecer la contraseña porque no cumple la política de Identity.");
        return 9;
    }

    dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
    {
        BeneficiaryUserId = user.Id,
        EventType = "LocalPasswordResetCli",
        Result = "Succeeded",
        ResourceType = nameof(IamUsuario),
        ResourceId = user.Id.ToString(),
        AfterJson = JsonSerializer.Serialize(new
        {
            ActorTechnical = actorTechnical,
            Environment = environmentName,
            Server = connection.DataSource,
            Database = connection.InitialCatalog,
            Action = "LocalPasswordReset",
            Result = "Succeeded"
        }),
        Justification = "Recuperación administrativa CLI.",
        CorrelationId = Guid.NewGuid().ToString("N")
    });
    await dbContext.SaveChangesAsync();
    Console.WriteLine("Contraseña restablecida y cuenta desbloqueada correctamente.");
    return 0;
}
catch
{
    Console.Error.WriteLine("No fue posible completar la operación administrativa.");
    return 10;
}

static string ReadSecret()
{
    var value = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace && value.Length > 0) value.Length--;
        else if (!char.IsControl(key.KeyChar)) value.Append(key.KeyChar);
    }

    return value.ToString();
}

public sealed class ProgramMarker;
