param (
    [string]$Server = "tcp:sql.imaginart.io,1433",
    [string]$Database = "db-landscape-tsi-dev-v2",
    [string]$UserId = $env:LANDSCAPE_TSI_DB_USER,
    [string]$Password = $env:LANDSCAPE_TSI_DB_PASSWORD
)

$connStr = "Server=$Server;Database=$Database;User Id=$UserId;Password=$Password;Encrypt=True;TrustServerCertificate=False"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$sql = @"
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[TProcesoAdopcionEmpresa]') 
      AND name = N'comentarioCapacidades'
)
BEGIN
    ALTER TABLE dbo.TProcesoAdopcionEmpresa ADD comentarioCapacidades NVARCHAR(1000) NULL;
    PRINT 'Columna comentarioCapacidades agregada a TProcesoAdopcionEmpresa.';
END
ELSE
BEGIN
    PRINT 'Columna comentarioCapacidades ya existia.';
END
"@
$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$cmd.ExecuteNonQuery()
Write-Output "Verificando estructura de TProcesoAdopcionEmpresa:"
$checkCmd = $conn.CreateCommand()
$checkCmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TProcesoAdopcionEmpresa'"
$reader = $checkCmd.ExecuteReader()
while ($reader.Read()) {
    Write-Output " - $($reader[0]) ($($reader[1]))"
}
$reader.Close()
$conn.Close()
