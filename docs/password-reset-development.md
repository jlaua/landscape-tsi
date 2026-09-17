# Restablecimiento administrativo de contraseña

La utilidad aislada para `jean` utiliza `UserManager<IamUsuario>` y las
operaciones `FindByNameAsync`, `GeneratePasswordResetTokenAsync` y
`ResetPasswordAsync`. No recibe la contraseña como argumento y no la escribe
en archivos ni en logs.

Desde la raíz del repositorio, con la conexión configurada en User Secrets o
variables de entorno y un actor técnico explícito:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:LANDSCAPE_TSI_PASSWORD_RESET_ACTOR = "operador-tecnico-autorizado"
dotnet run --project ".\tools\Landscape.Tsi.PasswordReset\Landscape.Tsi.PasswordReset.csproj"
```

La política actual permite `Development` y `Staging` únicamente contra
`db-landscape-tsi-dev`. En `Staging` exige escribir exactamente `RESET <usuario>`
usando el usuario solicitado, después de mostrar ambiente, servidor, base y usuario. `Production` se rechaza
hasta que exista una base productiva autorizada explícitamente en la política.

La herramienta valida la base mediante la infraestructura compartida, registra
un evento de auditoría con metadatos sanitizados sin contraseña ni hash y
solicita la nueva contraseña de forma interactiva, aplicando la política de
ASP.NET Core Identity. No recibe contraseñas como argumentos y no crea
endpoints públicos.

No se aceptan nombres de base arbitrarios. La administración Web
`Usuarios.RestablecerPassword` sigue siendo el mecanismo normal; esta CLI es
un mecanismo excepcional de recuperación.
