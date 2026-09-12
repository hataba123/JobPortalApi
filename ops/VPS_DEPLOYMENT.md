# Triển khai VPS bằng Docker

1. Tạo đăng nhập SQL Server riêng cho ứng dụng với quyền tối thiểu trên database JobPortal. Đặt connection string đó vào `APP_DB_CONNECTION`; không dùng tài khoản `sa` trong API hoặc worker.
2. Tạo `.env` từ `.env.production.example`, bổ sung `APP_ORIGIN`, `APP_DB_CONNECTION`, `EMAIL_WEBHOOK_URL`, chứng chỉ TLS và mọi secret. Không commit file này.
3. Áp dụng migration một lần bằng image ứng dụng và `dotnet ef database update`; sau đó chạy `docker compose -f docker-compose.production.yml up -d --build --scale api=2`.
4. Kiểm tra `/health` và `/ready` qua HTTPS. Chỉ Nginx xuất cổng ra Internet; SQL, Redis, ClamAV, API, worker và volumes ở network nội bộ. Redis dùng mật khẩu trong `REDIS_PASSWORD` để chia sẻ hạn mức giữa các instance API.
5. Sao lưu SQL Server và hai volumes `private-cv-data`, `public-media-data`, `data-protection-keys` sang nơi ngoài VPS mỗi 15 phút. Thực hành khôi phục trước khi đưa vào vận hành.

Worker được tách thành service riêng. Các API instance chỉ phục vụ HTTP; outbox dùng lease SQL nên một message chỉ được một worker nhận trong thời hạn lease. Khi worker dừng, message được nhận lại sau khi lease hết hạn.
