param (
    [string]$Server = "tcp:sql.imaginart.io,1433",
    [string]$Database = "db-landscape-tsi-dev-v2",
    [string]$UserId = $env:LANDSCAPE_TSI_DB_USER,
    [string]$Password = $env:LANDSCAPE_TSI_DB_PASSWORD
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($UserId) -or [string]::IsNullOrWhiteSpace($Password)) {
    Write-Output "Credenciales no configuradas en variables de entorno. Usando parametros locales si estan definidos."
}

$connStr = "Server=$Server;Database=$Database;User Id=$UserId;Password=$Password;Encrypt=True;TrustServerCertificate=False"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$sql = @"
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TProcesoAdopcionTSI]') 
      AND name = N'driversReporteVencimiento'
)
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionTSI ADD driversReporteVencimiento NVARCHAR(1000) NULL;
    PRINT 'Columna driversReporteVencimiento agregada a TProcesoAdopcionTSI.';
END
ELSE
BEGIN
    PRINT 'Columna driversReporteVencimiento ya existia.';
END
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$cmd.ExecuteNonQuery()

Write-Output "Verificando columnas en TProcesoAdopcionTSI:"
$checkCmd = $conn.CreateCommand()
$checkCmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TProcesoAdopcionTSI' AND COLUMN_NAME = 'driversReporteVencimiento'"
$reader = $checkCmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output "OK: $($reader[0]) ($($reader[1]) max: $($reader[2]))"
}
$reader.Close()
$conn.Close()
