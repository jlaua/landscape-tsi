# Autenticación local de desarrollo

Landscape TSI admite una cuenta local de respaldo mediante ASP.NET Core Identity y, cuando está configurado, el ingreso corporativo OAuth/OpenID Connect. Ambos mecanismos resuelven el mismo usuario interno y usan las mismas políticas de permisos del servidor.

## Configurar el bootstrap

El bootstrap está habilitado únicamente en `Development` por la configuración versionada. Configure una contraseña temporal fuera del repositorio:

```powershell
dotnet user-secrets set "BootstrapAdmin:Password" "<secreto>" --project src/Landscape.Tsi.Web
```

Como alternativa para la sesión actual de PowerShell:

```powershell
$env:LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD = "<secreto>"
```

Después ejecute:

```powershell
dotnet run --project src/Landscape.Tsi.Web
```

Si el secreto está disponible y las cuentas aún no existen, se crean `jean` y `administrador` y se les asigna `Administrador del Sistema`. Las ejecuciones posteriores no duplican cuentas ni sustituyen contraseñas. Una cuenta preexistente que no fue creada por bootstrap no se modifica ni se eleva automáticamente.

Si falta el secreto, la aplicación continúa arrancando y omite solamente el bootstrap. Nunca genera una contraseña predeterminada.

## Base de datos permitida

Sin `ConnectionStrings:LandscapeTsiDb`, el arranque local usa almacenamiento en memoria y no toca SQL Server. Si se configura SQL Server, `DatabaseSafety:ExpectedDatabaseName` debe declarar el catálogo esperado para el ambiente; la aplicación compara ese valor con `Initial Catalog` mediante igualdad exacta y detiene el arranque ante cualquier diferencia. En Azure puede establecerse, por ejemplo, mediante `DatabaseSafety__ExpectedDatabaseName=db-landscape-tsi-dev-v2`. Las migraciones se generan para revisión y no se ejecutan automáticamente.

Está prohibido aplicar migraciones o ejecutar DDL/DML contra `db-landscape-tsi`.

## OAuth corporativo

Active `Authentication:OAuth:Enabled` solo cuando el proveedor, `Authority`, `ClientId` y el vínculo emisor-sujeto estén aprobados y configurados mediante secretos o variables del entorno. No guarde el secreto del cliente ni tokens en archivos versionados o logs.

## Rotación

Tras el bootstrap inicial, verifique las cuentas, rote la contraseña por el procedimiento administrativo aprobado y desactive `BootstrapAdmin:Enabled` cuando deje de ser necesario. Elimine el secreto temporal de User Secrets o de la sesión del entorno.
