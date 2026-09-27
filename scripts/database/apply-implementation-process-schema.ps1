# Execute evolve-implementation-process-schema.sql securely using connection string from user-secrets or environment variable
$ErrorActionPreference = "Stop"

$connStr = $env:LANDSCAPE_TSI_DB_CONNECTION
if ([string]::IsNullOrWhiteSpace($connStr)) {
    $secretsJson = dotnet user-secrets list --project "$PSScriptRoot/../../src/Landscape.Tsi.Web"
    $match = $secretsJson | Where-Object { $_ -match "^ConnectionStrings:LandscapeTsiDb\s*=\s*(.+)$" }
    if ($match) {
        $connStr = ($match -replace "^ConnectionStrings:LandscapeTsiDb\s*=\s*", "").Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($connStr)) {
    throw "No se pudo obtener la cadena de conexión de user-secrets ni de LANDSCAPE_TSI_DB_CONNECTION."
}

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    Write-Output "Conectado exitosamente a: $($conn.Database)"

    $scriptPath = Join-Path $PSScriptRoot "evolve-implementation-process-schema.sql"
    $scriptContent = Get-Content -Path $scriptPath -Raw

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $scriptContent
    $cmd.ExecuteNonQuery()
    Write-Output "Script evolve-implementation-process-schema.sql ejecutado con éxito."

    # Validar columnas agregadas
    $checkCmd = $conn.CreateCommand()
    $checkCmd.CommandText = @"
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (TABLE_NAME = 'TProcesoAdopcionTSI' AND COLUMN_NAME IN ('tipoProceso', 'idFamilia', 'idTecnologiaEstandar', 'idProcesoEvaluacionOrigen', 'fechaInicioVigenciaContrato', 'fechaFinVigenciaContrato', 'numeroContratoCorporativo'))
   OR (TABLE_NAME = 'TProcesoAdopcionEmpresa' AND COLUMN_NAME IN ('fechaInicioAdopcion', 'especialistaTecnico', 'idContactoEspecialista', 'urlArquitectura', 'rutaArchivoArquitectura', 'nombreArchivoArquitectura', 'estadoImplementacion', 'tecnologiaAsIs'))
ORDER BY TABLE_NAME, COLUMN_NAME;
"@
    $reader = $checkCmd.ExecuteReader()
    Write-Output "`nColumnas verificadas en esquema:"
    while ($reader.Read()) {
        Write-Output "  [$($reader[0])] $($reader[1]) ($($reader[2])) Nullable: $($reader[4])"
    }
    $reader.Close()
}
finally {
    $conn.Close()
}
