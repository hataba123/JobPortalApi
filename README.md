# JobPortal API (ASP.NET Core)

API backend ASP.NET Core của JobPortal, sử dụng SQL Server và Entity Framework Core. Repository này là một service độc lập, có controller, service, DTO và migration riêng; frontend có thể kết nối tới API thông qua `BACKEND_API_URL` khi các contract tương thích.

| Thông tin | Giá trị |
| --- | --- |
| Framework | ASP.NET Core 8 |
| Ngôn ngữ | C# |
| ORM | Entity Framework Core |
| Cơ sở dữ liệu | SQL Server |
| Xác thực | JWT Bearer, phân quyền theo role |
| Tài liệu API | Swagger/OpenAPI |
| Test | xUnit |
| Cổng Docker | `8080` |
| Cổng local theo launch profile | HTTP `5042`, HTTPS `7146` |

## Chức năng

- Đăng ký, đăng nhập, OAuth, đổi mật khẩu và đặt lại mật khẩu.
- Phân quyền cho `Admin`, `Recruiter` và `Candidate`.
- Quản lý tin tuyển dụng, đơn ứng tuyển và trạng thái ứng tuyển.
- Quản lý công ty, danh mục nghề nghiệp, hồ sơ ứng viên và CV.
- Dashboard cho quản trị viên và nhà tuyển dụng.
- Tìm kiếm ứng viên và matching việc làm - ứng viên.
- Blog, đánh giá công ty, thông báo và việc làm đã lưu.
- Gói dịch vụ, credit và tích hợp VNPay sandbox.
- Health check, readiness check, rate limit, logging và correlation ID.

## Kiến trúc thư mục

```text
JobPortalApi/
├── Controllers/        # HTTP endpoints theo nhóm nghiệp vụ
├── Services/
│   ├── Admin/           # Nghiệp vụ quản trị
│   ├── User/            # Nghiệp vụ người dùng, recruiter, candidate
│   ├── Matching/        # Matching việc làm - ứng viên
│   ├── Payments/        # Thanh toán và credit
│   ├── Notifications/   # Gửi thông báo
│   └── Infrastructure/ # Seed dữ liệu và hạ tầng
├── DTOs/                # Request/response models
├── Models/              # Entity và enum
├── Data/                # ApplicationDbContext
├── Migrations/          # EF Core migrations
├── Middleware/          # Xử lý exception dùng chung
└── Program.cs           # DI, middleware, auth, CORS, Swagger
JobPortalApi.Tests/      # Unit test và test logic tích hợp
```

## Yêu cầu môi trường

- .NET 8 SDK.
- SQL Server 2019 trở lên hoặc Docker Desktop.
- Git.

## Cài đặt và chạy local

### 1. Lấy mã nguồn

```bash
git clone https://github.com/hataba123/JobPortalApi.git
cd JobPortalApi
dotnet restore JobPortalApi.sln
```

### 2. Cấu hình ứng dụng

`JobPortalApi/appsettings.json` có connection string local mẫu. Trước khi chạy cần cung cấp ít nhất các khóa JWT:

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=JobPortal;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Jwt__Key = "thay-bang-secret-ngau-nhien-toi-thieu-32-ky-tu"
$env:Jwt__Issuer = "JobPortalAPI"
$env:Jwt__Audience = "JobPortalClient"
```

Có thể cấu hình thêm VNPay:

```powershell
$env:VNPAY_TMN_CODE = "ma-vnpay-sandbox"
$env:VNPAY_HASH_SECRET = "secret-vnpay-sandbox"
$env:VNPAY_RETURN_URL = "http://localhost:3000/vi/payment/return"
```

Lưu ý: file `.env.example` dùng cho Docker Compose; ASP.NET Core không tự đọc file `.env` khi chạy trực tiếp bằng `dotnet run`.

### 3. Cập nhật database

Nếu máy chưa có `dotnet-ef`, cài một lần:

```bash
dotnet tool install --global dotnet-ef
```

Chạy migration:

```bash
dotnet ef database update --project JobPortalApi/JobPortalApi.csproj --startup-project JobPortalApi/JobPortalApi.csproj
```

### 4. Khởi động API

```bash
dotnet run --project JobPortalApi/JobPortalApi.csproj --launch-profile http
```

Các địa chỉ development:

- API HTTP: <http://localhost:5042>
- Swagger UI: <http://localhost:5042/swagger>
- Health: <http://localhost:5042/health>
- Readiness: <http://localhost:5042/ready>

Có thể dùng profile `https` để chạy đồng thời HTTP và HTTPS:

```bash
dotnet run --project JobPortalApi/JobPortalApi.csproj --launch-profile https
```

## Chạy bằng Docker Compose

Tạo `.env` từ `.env.example`, sau đó điền các biến bắt buộc:

```bash
docker compose up --build
```

Compose khởi động SQL Server ở cổng `1433` và API ở cổng `8080`. Chuỗi kết nối nội bộ dùng service name `sqlserver`, không dùng `localhost`.

Các biến Compose quan trọng:

| Biến | Mô tả |
| --- | --- |
| `MSSQL_SA_PASSWORD` | Mật khẩu tài khoản `sa` của SQL Server |
| `JWT_KEY` | Khóa ký JWT |
| `JWT_ISSUER` | JWT issuer, mặc định `JobPortal` |
| `JWT_AUDIENCE` | JWT audience, mặc định `JobPortalClient` |
| `VNPAY_TMN_CODE` | Mã merchant VNPay sandbox |
| `VNPAY_HASH_SECRET` | Khóa ký VNPay sandbox |
| `VNPAY_RETURN_URL` | URL frontend nhận kết quả thanh toán |

## Nhóm API chính

Các endpoint nghiệp vụ dùng tiền tố `/api`:

| Nhóm | Một số endpoint tiêu biểu |
| --- | --- |
| Xác thực | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` |
| Việc làm | `GET /api/jobpost`, `GET /api/jobpost/{id}`, `POST /api/jobpost` |
| Ứng tuyển | `POST /api/jobapplication`, `GET /api/jobapplication/my-jobs` |
| Công ty và danh mục | `GET /api/companies`, `GET /api/categories` |
| Hồ sơ và CV | `/api/candidate-profile/*` |
| Nhà tuyển dụng | `/api/recruiter/company`, `/api/recruiter/dashboard/*` |
| Quản trị | `/api/admin/users`, `/api/admin/companies`, `/api/admin/jobposts` |
| Nội dung | `/api/blogs`, `/api/reviews`, `/api/notifications` |
| Matching | `/api/matches/*` |
| Thanh toán | `/api/plans`, `/api/payment-orders`, `/api/credits/*` |

Swagger hiển thị đầy đủ request, response và quyền truy cập của từng endpoint.

## Seed dữ liệu development

Seeder chỉ chạy khi `Seed:Enabled=true` hoặc biến môi trường `SEED_DATABASE=true`. Seeder yêu cầu `SEED_PASSWORD` và tạo dữ liệu demo cho ba role:

```powershell
$env:SEED_DATABASE = "true"
$env:SEED_PASSWORD = "mat-khau-demo-chi-dung-local"
dotnet run --project JobPortalApi/JobPortalApi.csproj --launch-profile http
```

Không bật seed trong production nếu chưa đánh giá kỹ dữ liệu và quyền truy cập.

## Kiểm thử và build

```bash
dotnet build JobPortalApi.sln
dotnet test JobPortalApi.sln
```

Các test hiện có tập trung vào matching engine, chữ ký VNPay và private CV storage. Khi sửa service hoặc controller, bổ sung test tương ứng trong `JobPortalApi.Tests`.

## Bảo mật và vận hành

- Không commit JWT key, mật khẩu SQL Server, secret VNPay hoặc token.
- Dùng biến môi trường hoặc secret store cho cấu hình production.
- CORS hiện cho phép frontend local ở cổng `3000`, `3001` và `3002`; cần cập nhật policy khi triển khai domain mới.
- CV được chặn truy cập trực tiếp qua `/uploads/cv`; việc tải CV phải đi qua endpoint có kiểm tra quyền.
- JWT kiểm tra issuer, thời hạn, chữ ký và phiên bản mật khẩu.
- Rate limit riêng cho nhóm xác thực và thanh toán.
- Bật HTTPS ở môi trường production và sử dụng connection string có thông tin xác thực an toàn.

## Liên kết các thành phần

- Frontend: <https://github.com/hataba123/jobportal-fe>
- Backend NestJS: <https://github.com/hataba123/jobportal-be>

## Đóng góp

1. Tạo branch theo tính năng hoặc bug.
2. Giữ thay đổi đúng lớp `Controller`, `Service`, `DTO`, `Model` hoặc `Data` liên quan.
3. Tạo migration khi thay đổi entity/schema và kiểm tra migration trên database local.
4. Chạy `dotnet build` và `dotnet test` trước khi mở Pull Request.
5. Dùng commit message theo quy ước `feat:`, `fix:`, `docs:`, `test:` hoặc `chore:`.
