param (
    [string]$ServerInstance = "localhost\SQLEXPRESS",
    [string]$Database = "MealShopDb",
    [string]$BackupPath = "D:\HocTap\Pj1\Food\MealShopDb.bak"
)

Write-Host "--- Bat dau sao luu CSDL $Database tren may chu $ServerInstance ---" -ForegroundColor Cyan
Write-Host "File sao luu dich: $BackupPath"

# Thuc thi lenh BACKUP DATABASE thong qua cong cu sqlcmd cua SQL Server
# WITH FORMAT, INIT: ghi de bo sao luu moi nhat
# STATS = 10: hien thi tien do sau moi 10%
$sqlCommand = "BACKUP DATABASE [$Database] TO DISK = N'$BackupPath' WITH FORMAT, INIT, NAME = N'$Database-Full Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10;"

try {
    sqlcmd -S $ServerInstance -E -Q $sqlCommand
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Sao luu CSDL thanh cong vao $BackupPath!" -ForegroundColor Green
    } else {
        Write-Error "Sao luu that bai. Ma loi: $LASTEXITCODE"
    }
} catch {
    Write-Error "Loi khi goi sqlcmd: $_"
}