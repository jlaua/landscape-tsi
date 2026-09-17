param (
    [string]$Server = "tcp:sql.imaginart.io,1433",
    [string]$Database = "db-landscape-tsi-dev-v2",
    [string]$UserId = $env:LANDSCAPE_TSI_DB_USER,
    [string]$Password = $env:LANDSCAPE_TSI_DB_PASSWORD
)

$connStr = "Server=$Server;Database=$Database;User Id=$UserId;Password=$Password;Encrypt=True;TrustServerCertificate=False"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$script = Get-Content -Path "d:\JLAU\landscape tsi\scripts\database\evolve-company-capabilities-matrix.sql" -Raw
$cmd = $conn.CreateCommand()
$cmd.CommandText = $script
$cmd.ExecuteNonQuery()
Write-Output "Script ejecutado con exito."
$checkCmd = $conn.CreateCommand()
$checkCmd.CommandText = "SELECT COUNT(*) FROM dbo.TProcesoEmpresaCapacidad"
$count = $checkCmd.ExecuteScalar()
Write-Output "Registros en TProcesoEmpresaCapacidad: $count"
$conn.Close()
