[CmdletBinding()]
param(
    [string]$OutputDirectory = $(if ($env:BACKUP_DIR) { $env:BACKUP_DIR } else { Join-Path (Get-Location) "backups" })
)

$ErrorActionPreference = "Stop"

$requiredVariables = @("SQLSERVER_HOST", "SQLSERVER_DATABASE", "SQLSERVER_USER", "SQLCMDPASSWORD")
$missingVariables = foreach ($variable in $requiredVariables) {
    $value = (Get-Item -LiteralPath "Env:$variable" -ErrorAction SilentlyContinue).Value
    if ([string]::IsNullOrWhiteSpace($value)) { $variable }
}
if ($missingVariables.Count -gt 0) {
    throw "Thiếu biến môi trường kết nối SQL Server: $($missingVariables -join ', ')."
}

$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($null -eq $sqlcmd) {
    throw "Không tìm thấy sqlcmd trong PATH. Hãy cài SQL Server command-line tools trước khi chạy backup."
}

$resolvedDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $resolvedDirectory -Force | Out-Null

$timestamp = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssZ")
$targetFile = Join-Path $resolvedDirectory "jobportal-sqlserver-$timestamp.bak"
$escapedDatabase = $env:SQLSERVER_DATABASE.Replace("]", "]]" )
$escapedPath = $targetFile.Replace("'", "''")
$backupSql = "BACKUP DATABASE [$escapedDatabase] TO DISK = N'$escapedPath' WITH COPY_ONLY, COMPRESSION, CHECKSUM, INIT;"

# SQLCMDPASSWORD được sqlcmd đọc từ môi trường, không đưa mật khẩu vào tham số lệnh.
& $sqlcmd.Source `
    -S $env:SQLSERVER_HOST `
    -d master `
    -U $env:SQLSERVER_USER `
    -Q $backupSql `
    -b `
    -r 1 `
    -C

if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd backup thất bại với mã lỗi $LASTEXITCODE."
}

if (Test-Path -LiteralPath $targetFile) {
    Get-Item -LiteralPath $targetFile | Select-Object FullName, Length, LastWriteTimeUtc
} else {
    Write-Warning "SQL Server đã nhận lệnh backup nhưng file không nằm trên filesystem hiện tại. Kiểm tra quyền/path của máy chủ SQL Server."
}
