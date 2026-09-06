# Restablecimiento temporal de contraseña en Development

El procedimiento temporal para `jean` utiliza `UserManager<IamUsuario>` y las
operaciones `FindByNameAsync`, `GeneratePasswordResetTokenAsync` y
`ResetPasswordAsync`. No recibe la contraseña como argumento y no la escribe
en archivos ni en logs.

Desde la raíz del repositorio, con la conexión de desarrollo configurada en
User Secrets o variables de entorno:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project ".\tools\Landscape.Tsi.PasswordReset\Landscape.Tsi.PasswordReset.csproj"
```

La herramienta solo permite ejecutarse en `Development`, valida que la
conexión tenga como base `db-landscape-tsi-dev` mediante la infraestructura
compartida y registra un evento de auditoría sin contraseña ni hash. Solicita
la nueva contraseña de forma interactiva y la valida con la política de
ASP.NET Core Identity.

No usar este procedimiento contra `db-landscape-tsi` ni automatizarlo en
producción.
