param (
    [string]$Server = "tcp:sql.imaginart.io,1433",
    [string]$Database = "db-landscape-tsi-dev-v2",
    [string]$UserId = $env:LANDSCAPE_TSI_DB_USER,
    [string]$Password = $env:LANDSCAPE_TSI_DB_PASSWORD
)

$ErrorActionPreference = "Stop"

$connStr = "Server=$Server;Database=$Database;User Id=$UserId;Password=$Password;Encrypt=True;TrustServerCertificate=False"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

Write-Output "=== CONEXION ESTABLECIDA CON $Database ==="

$companiesData = @(
    @{
        ImplId = 121
        Name = "Mi Banco Colombia"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.005000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "0.100000" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = $null },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "6" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "2" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "Cuenta con licencia activa para: no brindo informacion" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "no aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "10.51421" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "10.51421" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "0.252" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "sin respuesta" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "sin respuesta" }
        )
    },
    @{
        ImplId = 131
        Name = "Yape"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.0195912" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "0.037041" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = $null },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "11" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "no brindo informacion" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = $null },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "0.012197" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "1970.909" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "1970.921197" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "3.020" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "No Aplica" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "No Aplica" }
        )
    },
    @{
        ImplId = 122
        Name = "Mi Banco Peru"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.002900" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "1" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "10" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "11" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "5 (MTLS)" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "78.67" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "123.49" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "120" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "243.49" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "0.676 TB" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = $null },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "676.24 GB" }
        )
    },
    @{
        ImplId = 128
        Name = "Tenpo Chile"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.0018700" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "8" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "47" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "47" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "Cuenta con licencia Activa" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "no aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "1250" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "1250" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "sin respuesta" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "sin respuesta" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "sin respuesta" }
        )
    },
    @{
        ImplId = 118
        Name = "Credicorp Capital"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.012000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "19" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "160" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "107" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "2" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "6.09" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "sin informacion" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "20.07" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "20.07" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "26.970" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "sin respuesta" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "sin respuesta" }
        )
    },
    @{
        ImplId = 130
        Name = "YAPE Bolivia"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.070000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "0.150000" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "1" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "1" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "0" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "No cuenta con modulo;" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "No Aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "2.25" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "2.25" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "4.750" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "180 KB" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "400 KB" }
        )
    },
    @{
        ImplId = 116
        Name = "BCP Bolivia"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.060000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "0.060000" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = $null },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "6" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "2" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "No cuenta con modulo;" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "No Aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "1.25" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "1.25" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "3.750" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "170 KB" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "395 KB" }
        )
    },
    @{
        ImplId = 125
        Name = "Pacifico Salud/EPS"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.013010" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "3" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "13" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "16" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "41 Prod / 118 Test" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "No cuenta con modulo" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "6.23" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "0.061" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "6.291" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "0.19" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = $null },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = $null }
        )
    },
    @{
        ImplId = 124
        Name = "Pacifico Prestadoras de Salud"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.066360" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "20" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "58" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "78" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "No cuenta con modulo" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "No cuenta con modulo" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "65" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "391.96" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "456.96" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "14.66" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = $null },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = $null }
        )
    },
    @{
        ImplId = 117
        Name = "BCP Miami"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.000010" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "1" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "0" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "6" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "0" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "0" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "no aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "0.15963" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "0.15963" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "3.347" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "sin respuesta" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "sin respuesta" }
        )
    },
    @{
        ImplId = 126
        Name = "Pacifico Seguros"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.020000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser PaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "2" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "28" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "33" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "(01) api.pacifico.com.pe" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "187" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "70" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "70" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "140" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "5.400" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "165.9 GB" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "5.4 TB" }
        )
    },
    @{
        ImplId = 127
        Name = "Prima AFP"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.060000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser PaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "2" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "17" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "21" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "(01) api.turesumenprima." },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "54" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "426" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "52" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "478" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "19.401" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "442.9 GB" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "19.4 TB" }
        )
    },
    @{
        ImplId = 119
        Name = "Culqi"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.0150000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = "3" },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = "76" },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "43" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "33" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "111.9" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "no aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "181.6" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "181.6" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "4.530" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "sin respuesta" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "sin respuesta" }
        )
    },
    @{
        ImplId = 123
        Name = "Monokera"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.0001440" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = $null },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "19" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "7" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "3.87" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "no aplica" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "6.08" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "6.08" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "0.000" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = $null },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = $null }
        )
    },
    @{
        ImplId = 129
        Name = "TYBA"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.0150000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = $null },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "15" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "15" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "13M" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "13" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "13" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "26" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "sin respuesta" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "sin respuesta" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "sin respuesta" }
        )
    },
    @{
        ImplId = 120
        Name = "iO"
        Drivers = @(
            @{ Name = "Throughput (mensual)"; Unit = "Gbps"; Value = "0.0089000" },
            @{ Name = "Ancho de Banda (mensual)"; Unit = "Gbps"; Value = "no aplica por ser SaaS" },
            @{ Name = "Dominio"; Unit = "Dominio"; Value = $null },
            @{ Name = "Sub Dominios"; Unit = "Sub Dominios"; Value = $null },
            @{ Name = "Cantidad de aplicaciones (FQDN)"; Unit = "FQDN"; Value = "6" },
            @{ Name = "Cantidad de Aplicaciones FQDN en API Security"; Unit = "FQDN"; Value = "61" },
            @{ Name = "API Protection / API Security Request"; Unit = "M x Mes"; Value = "No cuenta con modulo" },
            @{ Name = "Millones de Request (mensual / antibot)"; Unit = "Millones"; Value = "0.05" },
            @{ Name = "Millones Request WAF (mensual)"; Unit = "Millones"; Value = "163.6" },
            @{ Name = "Millones Request Antibot+WAF (mensual)"; Unit = "Millones"; Value = "163.65" },
            @{ Name = "Data Transfer (mensual)"; Unit = "TB"; Value = "2.617" },
            @{ Name = "Request Size (mensual)"; Unit = "KB / GB"; Value = "818 GB" },
            @{ Name = "Response Size (mensual)"; Unit = "KB / GB / TB"; Value = "4951 GB" }
        )
    }
)

function Parse-Cantidad($val) {
    if ([string]::IsNullOrWhiteSpace($val)) { return [DBNull]::Value }
    $trimmed = $val.Trim()
    if ($trimmed -match '^[0-9]+(\.[0-9]+)?$') {
        $d = [decimal]::Parse($trimmed, [System.Globalization.CultureInfo]::InvariantCulture)
        return [Math]::Round($d, 2)
    }
    return [DBNull]::Value
}

$tran = $conn.BeginTransaction("SeedDrivers")

try {
    # 1. Limpiar drivers previos
    $implIds = ($companiesData | ForEach-Object { $_.ImplId }) -join ","
    $delCmd = $conn.CreateCommand()
    $delCmd.Transaction = $tran
    $delCmd.CommandText = "DELETE FROM TDriver WHERE idTecnologiaTSIimplementadaSubsidiaria IN ($implIds)"
    $deleted = $delCmd.ExecuteNonQuery()
    Write-Output "Drivers previos eliminados: $deleted"

    # 2. Insertar cada driver con SqlCommand parametrizado
    $insertSql = @'
INSERT INTO TDriver (
    idTecnologiaTSIimplementadaSubsidiaria,
    nombreDriver,
    descripcionDriver,
    unidadMedida,
    unidadDemetrica,
    cantidad,
    precioUnitario,
    moneda,
    frecuenciaDelCalculo,
    valor
) VALUES (
    @idImpl,
    @nombre,
    @descripcion,
    @unidadMedida,
    @unidadDemetrica,
    @cantidad,
    @precioUnitario,
    @moneda,
    @frecuencia,
    @valor
)
'@

    $totalInserted = 0

    foreach ($comp in $companiesData) {
        $implId = $comp.ImplId
        $compName = $comp.Name
        Write-Output "Procesando $compName (Impl: $implId)..."

        foreach ($drv in $comp.Drivers) {
            $cmd = $conn.CreateCommand()
            $cmd.Transaction = $tran
            $cmd.CommandText = $insertSql

            $drvName = $drv.Name
            $drvUnit = $drv.Unit
            $rawVal = $drv.Value

            $cant = Parse-Cantidad $rawVal

            $cmd.Parameters.AddWithValue("@idImpl", $implId) | Out-Null
            $cmd.Parameters.AddWithValue("@nombre", $drvName) | Out-Null
            $cmd.Parameters.AddWithValue("@descripcion", $drvName) | Out-Null
            $cmd.Parameters.AddWithValue("@unidadMedida", $(if ([string]::IsNullOrWhiteSpace($drvUnit)) { [DBNull]::Value } else { $drvUnit })) | Out-Null
            $cmd.Parameters.AddWithValue("@unidadDemetrica", $(if ([string]::IsNullOrWhiteSpace($drvUnit)) { [DBNull]::Value } else { $drvUnit })) | Out-Null

            $pCant = $cmd.Parameters.Add("@cantidad", [System.Data.SqlDbType]::Decimal)
            $pCant.Precision = 18
            $pCant.Scale = 2
            $pCant.Value = $cant

            $pPrecio = $cmd.Parameters.Add("@precioUnitario", [System.Data.SqlDbType]::Decimal)
            $pPrecio.Precision = 18
            $pPrecio.Scale = 2
            $pPrecio.Value = [decimal]0.00

            $cmd.Parameters.AddWithValue("@moneda", "USD") | Out-Null
            $cmd.Parameters.AddWithValue("@frecuencia", "Mensual") | Out-Null
            $cmd.Parameters.AddWithValue("@valor", $(if ($rawVal -eq $null) { [DBNull]::Value } else { $rawVal.ToString() })) | Out-Null

            $cmd.ExecuteNonQuery() | Out-Null
            $totalInserted++
        }
    }

    $tran.Commit()
    Write-Output "`n=== CARGA COMPLETADA CON EXITO ==="
    Write-Output "Total de drivers insertados: $totalInserted en $($companiesData.Count) empresas."
}
catch {
    $tran.Rollback()
    Write-Error "ERROR durante la insercion: $_"
    throw
}
finally {
    $conn.Close()
}
