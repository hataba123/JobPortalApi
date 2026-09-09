# Vận hành

## Backup SQL Server

Đặt `SQLSERVER_HOST`, `SQLSERVER_DATABASE`, `SQLSERVER_USER` và `SQLCMDPASSWORD`
trong secret store hoặc phiên chạy bảo mật. Có thể đặt thêm `BACKUP_DIR`; mặc định
file được ghi vào thư mục `backups` hiện tại.

```powershell
$env:SQLSERVER_HOST = "localhost,1433"
$env:SQLSERVER_DATABASE = "JobPortal"
$env:SQLSERVER_USER = "sa"
$env:SQLCMDPASSWORD = "<secret-không-commit>"
& .\ops\backup-sqlserver.ps1
```

Đường dẫn backup phải là đường dẫn mà tiến trình SQL Server có thể ghi. Nếu SQL Server
chạy trong container hoặc máy khác, hãy mount/chia sẻ thư mục tương ứng và kiểm tra file
`.bak` từ phía máy chủ cơ sở dữ liệu. Script dùng `COPY_ONLY`, `CHECKSUM`, không in mật
khẩu và không xóa bản backup cũ.
