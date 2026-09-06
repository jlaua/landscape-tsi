using Landscape.Tsi.Infrastructure;
using Landscape.Tsi.Infrastructure.Identity;
using Landscape.Tsi.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Este procedimiento solo puede ejecutarse con ASPNETCORE_ENVIRONMENT=Development.");
    return 2;
}

var builder = Host.CreateApplicationBuilder(args);
var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("src/Landscape.Tsi.Web/appsettings.json", optional: false)
    .AddJsonFile($"src/Landscape.Tsi.Web/appsettings.{environmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<ProgramMarker>(optional: true);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
using var host = builder.Build();
using var scope = host.Services.CreateScope();
var manager = scope.ServiceProvider.GetRequiredService<UserManager<Landscape.Tsi.Domain.Identity.IamUsuario>>();
var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
var user = await manager.FindByNameAsync("jean");
if (user is null)
{
    Console.Error.WriteLine("No se encontró el usuario objetivo.");
    return 3;
}

Console.WriteLine("Restablecimiento temporal de la contraseña de jean en Development.");
Console.Write("Nueva contraseña: ");
var password = ReadSecret();
Console.Write("Confirmar contraseña: ");
var confirmation = ReadSecret();
Console.WriteLine();
if (!string.Equals(password, confirmation, StringComparison.Ordinal))
{
    Console.Error.WriteLine("Las contraseñas no coinciden.");
    return 4;
}

var token = await manager.GeneratePasswordResetTokenAsync(user);
var result = await manager.ResetPasswordAsync(user, token, password);
if (!result.Succeeded)
{
    Console.Error.WriteLine("No se pudo restablecer la contraseña porque no cumple la política de Identity.");
    return 5;
}

dbContext.AuthorizationAuditEvents.Add(new IamEventoAuditoriaAutorizacion
{
    BeneficiaryUserId = user.Id,
    EventType = "LocalPasswordResetDevelopment",
    Result = "Succeeded",
    ResourceType = nameof(IamUsuario),
    ResourceId = user.Id.ToString(),
    AfterJson = "{\"CredentialUpdated\":true}",
    Justification = "Procedimiento temporal de desarrollo.",
    CorrelationId = Guid.NewGuid().ToString("N")
});
await dbContext.SaveChangesAsync();

Console.WriteLine("Restablecimiento completado mediante ASP.NET Core Identity.");
return 0;

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
