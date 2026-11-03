param (
    [string]$ServerInstance = "localhost\SQLEXPRESS",
    [string]$Database = "MealShopDb",
    [string]$BackupPath = "D:\HocTap\Pj1\Food\MealShopDb.bak"
)

Write-Host "--- Bat dau khoi phuc CSDL $Database tu file $BackupPath ---" -ForegroundColor Cyan

if (-not (Test-Path $BackupPath)) {
    Write-Error "File sao luu khong ton tai tai: $BackupPath"
    exit 1
}

# Luu y quan trong:
# 1. Chuyen CSDL sang SINGLE_USER voi ROLLBACK IMMEDIATE de ngat moi ket noi hien co (tranh loi database dang bi khoa boi ung dung)
# 2. RESTORE DATABASE ... WITH REPLACE de ghi de toan bo CSDL hien tai bang ban sao luu
# 3. Chuyen CSDL tro lai MULTI_USER de ung dung co the truy cap binh thuong
$sqlCommand = @"
ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$Database] FROM DISK = N'$BackupPath' WITH REPLACE;
ALTER DATABASE [$Database] SET MULTI_USER;
"@

try {
    sqlcmd -S $ServerInstance -E -Q $sqlCommand
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Khoi phuc CSDL $Database thanh cong tu file $BackupPath!" -ForegroundColor Green
    } else {
        Write-Error "Khoi phuc that bai. Ma loi: $LASTEXITCODE"
    }
} catch {
    Write-Error "Loi khi goi sqlcmd: $_"
}