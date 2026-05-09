param(
    [string]$SourceServer = "120.48.175.83,1433",
    [string]$SourceDatabase = "FaJuanDb",
    [string]$SourceUser = "sa",
    [Parameter(Mandatory = $true)]
    [string]$SourcePassword,
    [string]$OutputPath = "release/private/fajuan-foundation-data.mysql.sql"
)

$ErrorActionPreference = "Stop"

$tables = @(
    "AdminUser",
    "AdminRole",
    "AdminMenu",
    "AdminUserRole",
    "AdminRoleMenu",
    "AdminPermission",
    "AdminRolePermission",
    "MediaAsset",
    "Banner",
    "Store",
    "Product",
    "CouponTemplate",
    "CouponTemplateProductScope",
    "CouponTemplateStoreScope",
    "CouponPack",
    "CouponPackItem",
    "WeChatPaySetting"
)

function ConvertTo-MySqlLiteral {
    param([object]$Value)

    if ($null -eq $Value -or $Value -is [DBNull]) {
        return "NULL"
    }

    if ($Value -is [bool]) {
        if ($Value) { return "1" }
        return "0"
    }

    if ($Value -is [byte]) {
        return [string]$Value
    }

    if ($Value -is [int] -or $Value -is [long] -or $Value -is [decimal] -or $Value -is [double] -or $Value -is [float]) {
        return ([string]$Value).Replace(",", ".")
    }

    if ($Value -is [DateTime]) {
        return "'" + $Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "'"
    }

    $text = [string]$Value
    $text = $text.Replace("\", "\\").Replace("'", "''").Replace("`r", "\r").Replace("`n", "\n")
    return "'" + $text + "'"
}

function Invoke-DataTable {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    return ,$table
}

$directory = Split-Path -Parent $OutputPath
if ($directory) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$builder["Data Source"] = $SourceServer
$builder["Initial Catalog"] = $SourceDatabase
$builder["User ID"] = $SourceUser
$builder["Password"] = $SourcePassword
$builder["TrustServerCertificate"] = $true
$builder["Encrypt"] = $false

$connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
$connection.Open()

try {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("SET NAMES utf8mb4;")
    $lines.Add("SET time_zone = '+08:00';")
    $lines.Add("SET FOREIGN_KEY_CHECKS = 0;")
    $lines.Add("")
    $lines.Add("-- Generated from real MSSQL foundation data only. Do not commit this file when it contains payment secrets.")
    $lines.Add("-- Excludes AppUser, CouponOrder, PaymentTransaction, UserCoupon, CouponWriteOffRecord, and MiniAppShareEvent business history.")
    $lines.Add("")

    foreach ($tableName in $tables) {
        $exists = Invoke-DataTable -Connection $connection -Sql "SELECT 1 AS ExistsFlag FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '$tableName';"
        if ($exists.Rows.Count -eq 0) {
            throw "Source table dbo.$tableName does not exist."
        }

        $columns = Invoke-DataTable -Connection $connection -Sql "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '$tableName' ORDER BY ORDINAL_POSITION;"
        $columnNames = @($columns.Rows | ForEach-Object { [string]$_["COLUMN_NAME"] })
        $quotedColumns = ($columnNames | ForEach-Object { "[$_]" }) -join ", "
        $rows = Invoke-DataTable -Connection $connection -Sql "SELECT $quotedColumns FROM [dbo].[$tableName] ORDER BY [Id];"

        $lines.Add("-- $tableName rows: $($rows.Rows.Count)")
        if ($rows.Rows.Count -gt 0) {
            $mysqlColumns = ($columnNames | ForEach-Object { "``$_``" }) -join ", "
            foreach ($row in $rows.Rows) {
                $values = ($columnNames | ForEach-Object { ConvertTo-MySqlLiteral $row[$_] }) -join ", "
                $lines.Add("INSERT INTO ``$tableName`` ($mysqlColumns) VALUES ($values);")
            }
        }
        $lines.Add("")
    }

    $lines.Add("SET FOREIGN_KEY_CHECKS = 1;")
    Set-Content -LiteralPath $OutputPath -Value $lines -Encoding UTF8
    Write-Output "Wrote $OutputPath"
}
finally {
    $connection.Dispose()
}
