# JobPortalApi

## Giới thiệu tổng quan

**JobPortalApi** là một RESTful API được xây dựng bằng **.NET Core 8.0** phục vụ cho hệ thống tuyển dụng việc làm trực tuyến. Hệ thống hỗ trợ ba vai trò chính: **Admin**, **Recruiter** (nhà tuyển dụng), và **Candidate** (ứng viên), cung cấp đầy đủ các tính năng quản lý công việc, ứng viên, và quá trình tuyển dụng.

## Công nghệ sử dụng

### Backend Framework

- **.NET Core 8.0** - Framework chính
- **ASP.NET Core Web API** - Xây dựng RESTful API
- **Entity Framework Core** - ORM cho thao tác cơ sở dữ liệu
- **SQL Server** - Hệ quản trị cơ sở dữ liệu

### Authentication & Authorization

- **JWT (JSON Web Tokens)** - Xác thực và phân quyền
- **BCrypt** - Mã hóa mật khẩu
- **Role-based Authorization** - Phân quyền theo vai trò

### Packages chính

- **Microsoft.EntityFrameworkCore.SqlServer** - Kết nối SQL Server
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT Authentication
- **BCrypt.Net-Next** - Mã hóa mật khẩu
- **Swashbuckle.AspNetCore** - Swagger documentation

## Cấu trúc dự án

```
JobPortalApi/
├── Controllers/
│   ├── Admin/           # Controllers cho Admin
│   ├── Auth/           # Controllers xác thực
│   └── User/           # Controllers cho User/Recruiter/Candidate
├── Data/               # DbContext và cấu hình database
├── DTOs/               # Data Transfer Objects
│   ├── AdminCompany/
│   ├── AdminDashboard/
│   ├── AdminJobPost/
│   ├── AdminNotification/
│   ├── AdminReview/
│   ├── AdminUser/
│   ├── Apply/
│   ├── Blog/
│   ├── CandidateProfile/
│   ├── Category/
│   ├── Company/
│   ├── JobPost/
│   ├── RecruiterDashboard/
│   ├── Review/
│   ├── SavedJob/
│   └── Shared/
├── Migrations/         # Entity Framework migrations
├── Models/             # Domain models/entities
│   └── Enums/         # Enum definitions
├── Services/           # Business logic layer
│   ├── Admin/         # Services cho Admin
│   ├── Interface/     # Service interfaces
│   ├── User/          # Services cho User
│   └── Helpers/       # Helper services
├── Properties/         # Cấu hình project
└── wwwroot/           # Static files và uploads
    └── uploads/       # File uploads storage
```

## Mô hình dữ liệu (Database Schema)

### Các Entity chính

#### 1. User (Người dùng)

```csharp
- Id (Guid, PK)
- Email (string, unique)
- PasswordHash (string)
- FullName (string)
- Role (UserRole enum: Admin, Recruiter, Candidate)
- CreatedAt (DateTime)
```

#### 2. Company (Công ty)

```csharp
- Id (Guid, PK)
- Name (string)
- Logo (string)
- Description (string)
- Location (string)
- Employees (string)
- Industry (string)
- OpenJobs (int)
- Rating (double)
- Website (string)
- Founded (string)
- Tags (string)
- UserId (Guid?, FK to User)
```

#### 3. Category (Danh mục công việc)

```csharp
- Id (Guid, PK)
- Name (string)
- Icon (string)
- Color (string)
```

#### 4. JobPost (Bài đăng tuyển dụng)

```csharp
- Id (Guid, PK)
- Title (string)
- Description (string)
- SkillsRequired (string)
- Location (string)
- Salary (decimal)
- EmployerId (Guid, FK to User)
- CompanyId (Guid?, FK to Company)
- Logo (string)
- Type (string)
- Tags (List<string>)
- Applicants (int)
- CreatedAt (DateTime)
- CategoryId (Guid, FK to Category)
```

#### 5. CandidateProfile (Hồ sơ ứng viên)

```csharp
- Id (Guid, PK)
- UserId (Guid, FK to User)
- ResumeUrl (string?)
- Experience (string?)
- Skills (string?)
- Education (string?)
- Dob (DateTime?)
- Gender (string?)
- PortfolioUrl (string?)
- LinkedinUrl (string?)
- GithubUrl (string?)
- Certificates (string?)
- Summary (string?)
```

#### 6. Job (Đơn ứng tuyển)

```csharp
- Id (Guid, PK)
- JobPostId (Guid, FK to JobPost)
- CandidateId (Guid, FK to User)
- AppliedAt (DateTime)
- CVUrl (string)
- Status (ApplyStatus enum)
```

#### 7. SavedJob (Công việc đã lưu)

```csharp
- Id (Guid, PK)
- UserId (Guid, FK to User)
- JobPostId (Guid, FK to JobPost)
- SavedAt (DateTime)
```

#### 8. Review (Đánh giá công ty)

```csharp
- Id (Guid, PK)
- UserId (Guid, FK to User)
- CompanyId (Guid, FK to Company)
- Rating (int)
- Comment (string)
- CreatedAt (DateTime)
```

#### 9. Notification (Thông báo)

```csharp
- Id (Guid, PK)
- UserId (Guid, FK to User)
- Message (string)
- Read (bool)
- CreatedAt (DateTime)
- Type (string)
```

#### 10. Blog System (Hệ thống blog)

- **Blog**: Bài viết blog
- **BlogAuthor**: Tác giả blog
- **BlogCategory**: Danh mục blog
- **BlogView**: Lượt xem blog
- **BlogLike**: Lượt thích blog

### Enums

#### UserRole

```csharp
public enum UserRole
{
    Admin = 0,
    Recruiter = 1,
    Candidate = 2
}
```

#### ApplyStatus

```csharp
public enum ApplyStatus
{
    Pending,
    Reviewed,
    Accepted,
    Rejected
}
```

## API Endpoints

### 🔐 Authentication (`/api/auth`)

- `POST /register` - Đăng ký tài khoản
- `POST /login` - Đăng nhập
- `POST /refresh` - Làm mới token

### 👤 User Management

#### Admin User Management (`/api/admin/users`)

- `GET /` - Lấy danh sách tất cả người dùng _(Admin)_
- `GET /{id}` - Lấy thông tin người dùng theo ID _(Admin)_
- `POST /` - Tạo người dùng mới _(Admin)_
- `PUT /{id}` - Cập nhật thông tin người dùng _(Admin)_
- `DELETE /{id}` - Xóa người dùng _(Admin)_

### 🏢 Company Management

#### Admin Company Management (`/api/admin/companies`)

- `GET /` - Lấy danh sách tất cả công ty _(Admin)_
- `GET /{id}` - Lấy thông tin công ty theo ID _(Admin)_
- `POST /` - Tạo công ty mới _(Admin)_
- `PUT /{id}` - Cập nhật thông tin công ty _(Admin)_
- `DELETE /{id}` - Xóa công ty _(Admin)_

#### Recruiter Company Management (`/api/recruiter/company`)

- `GET /` - Lấy thông tin công ty của recruiter _(Recruiter)_
- `PUT /` - Cập nhật thông tin công ty _(Recruiter)_
- `DELETE /` - Xóa công ty _(Recruiter)_

### 💼 Job Post Management

#### Admin Job Post Management (`/api/admin/jobposts`)

- `GET /` - Lấy danh sách tất cả bài đăng _(Admin)_
- `GET /{id}` - Lấy thông tin bài đăng theo ID _(Admin)_
- `POST /` - Tạo bài đăng mới _(Admin)_
- `PUT /{id}` - Cập nhật bài đăng _(Admin)_
- `DELETE /{id}` - Xóa bài đăng _(Admin)_

#### User Job Post Management (`/api/jobpost`)

- `GET /` - Lấy danh sách tất cả bài đăng _(Public)_
- `GET /{id}` - Lấy thông tin bài đăng theo ID _(Public)_
- `GET /company/{companyId}` - Lấy bài đăng theo công ty _(Public)_
- `GET /category/{categoryId}` - Lấy bài đăng theo danh mục _(Public)_
- `GET /my-posts` - Lấy bài đăng của recruiter _(Recruiter)_
- `POST /` - Tạo bài đăng mới _(Recruiter)_
- `PUT /{id}` - Cập nhật bài đăng _(Recruiter)_
- `DELETE /{id}` - Xóa bài đăng _(Recruiter)_

### 📝 Job Application Management (`/api/jobapplication`)

- `POST /` - Ứng tuyển vào công việc _(Candidate)_
- `GET /job/{jobPostId}/candidates` - Lấy danh sách ứng viên cho một job _(Recruiter)_
- `GET /my-jobs` - Lấy danh sách job đã ứng tuyển _(Candidate)_
- `GET /` - Lấy tất cả đơn ứng tuyển _(Admin)_
- `GET /{id}` - Lấy thông tin đơn ứng tuyển theo ID _(Admin, Candidate)_
- `PUT /{id}/status` - Cập nhật trạng thái đơn ứng tuyển _(Admin, Recruiter)_
- `DELETE /{id}` - Xóa đơn ứng tuyển _(Admin)_

### 👨‍💼 Candidate Profile Management (`/api/candidate-profile`)

#### Self Management (Candidate)

- `GET /me` - Lấy hồ sơ cá nhân _(Candidate)_
- `PUT /me` - Cập nhật hồ sơ cá nhân _(Candidate)_
- `POST /me/upload-cv` - Upload CV _(Candidate)_
- `DELETE /me/delete-cv` - Xóa CV _(Candidate)_

#### Recruiter Functions

- `GET /recruiter/{id}` - Xem hồ sơ ứng viên _(Recruiter)_
- `GET /recruiter/{id}/applications` - Xem đơn ứng tuyển của ứng viên _(Recruiter)_
- `GET /recruiter/search` - Tìm kiếm ứng viên _(Recruiter)_
- `GET /recruiter/applied` - Lấy ứng viên đã ứng tuyển vào job của mình _(Recruiter)_

### 📊 Dashboard

#### Admin Dashboard (`/api/admin/dashboard`)

- `GET /` - Lấy thống kê tổng quan _(Admin)_

#### Recruiter Dashboard (`/api/recruiter/dashboard`)

- `GET /{recruiterId}` - Lấy thống kê cho recruiter _(Recruiter)_

### 🗂️ Categories (`/api/categories`)

- `GET /` - Lấy danh sách danh mục _(Public)_
- `GET /{id}` - Lấy thông tin danh mục theo ID _(Public)_

### 💾 Saved Jobs (`/api/savedjob`)

- `GET /` - Lấy danh sách công việc đã lưu _(Candidate)_
- `POST /` - Lưu công việc _(Candidate)_
- `DELETE /{id}` - Bỏ lưu công việc _(Candidate)_

### ⭐ Reviews

#### Admin Review Management (`/api/admin/reviews`)

- `GET /` - Lấy tất cả đánh giá _(Admin)_
- `GET /{id}` - Lấy đánh giá theo ID _(Admin)_
- `PUT /{id}` - Cập nhật đánh giá _(Admin)_
- `DELETE /{id}` - Xóa đánh giá _(Admin)_

#### User Review Management (`/api/review`)

- `GET /company/{companyId}` - Lấy đánh giá của công ty _(Public)_
- `POST /` - Tạo đánh giá mới _(User)_

### 🔔 Notifications (`/api/notification`)

- `GET /` - Lấy danh sách thông báo _(User)_
- `PUT /{id}/read` - Đánh dấu đã đọc _(User)_
- `DELETE /{id}` - Xóa thông báo _(User)_

### 📝 Blogs (`/api/blogs`)

- Hệ thống blog với các chức năng CRUD cơ bản
- Quản lý tác giả, danh mục, lượt xem và lượt thích

## Phân quyền và bảo mật

### Vai trò và quyền hạn

#### 🔧 Admin

- Quản lý toàn bộ hệ thống
- CRUD tất cả entities: Users, Companies, JobPosts, Reviews
- Xem dashboard tổng quan
- Quản lý đơn ứng tuyển và thay đổi trạng thái

#### 👔 Recruiter (Nhà tuyển dụng)

- Quản lý thông tin công ty của mình
- Tạo, sửa, xóa bài đăng tuyển dụng
- Xem danh sách ứng viên ứng tuyển
- Tìm kiếm và đánh giá ứng viên
- Thay đổi trạng thái đơn ứng tuyển
- Xem dashboard cá nhân

#### 👤 Candidate (Ứng viên)

- Quản lý hồ sơ cá nhân và CV
- Tìm kiếm và ứng tuyển công việc
- Lưu công việc yêu thích
- Xem lịch sử ứng tuyển
- Đánh giá công ty sau khi làm việc

### Bảo mật

- **JWT Authentication**: Xác thực bằng token
- **Role-based Authorization**: Phân quyền theo vai trò
- **Password Hashing**: Mã hóa mật khẩu bằng BCrypt
- **CORS**: Cấu hình Cross-Origin Resource Sharing
- **HTTPS**: Hỗ trợ SSL/TLS

## Cấu hình

### Database Connection

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DESKTOP-8CRV363\\SQLEXPRESS;Database=JobPortal;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### JWT Configuration

```json
{
  "Jwt": {
    "Key": "abcdefghijklmnopqrstuvwxyz123456",
    "Issuer": "JobPortalAPI"
  }
}
```

### CORS Policy

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "https://localhost:3001")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

## Cài đặt và chạy dự án

### Yêu cầu hệ thống

- **.NET 8.0 SDK** hoặc cao hơn
- **SQL Server** (LocalDB, Express, hoặc Full version)
- **Visual Studio 2022** hoặc **VS Code** (khuyến nghị)

### Các bước cài đặt

#### 1. Clone repository

```bash
git clone <repository-url>
cd JobPortalApi
```

#### 2. Restore packages

```bash
dotnet restore
```

#### 3. Cấu hình cơ sở dữ liệu

Cập nhật connection string trong `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=JobPortal;Trusted_Connection=True;"
  }
}
```

#### 4. Chạy migrations

```bash
dotnet ef database update
```

#### 5. Chạy ứng dụng

```bash
dotnet run
```

hoặc sử dụng Visual Studio:

- Mở `JobPortalApi.sln`
- Chọn profile "https" hoặc "IIS Express"
- Nhấn F5 để chạy

### Truy cập ứng dụng

- **API Base URL**: `https://localhost:7146` hoặc `http://localhost:5042`
- **Swagger Documentation**: `https://localhost:7146/swagger`

## API Documentation

### Swagger/OpenAPI

Dự án tích hợp sẵn Swagger UI để documentation và test API:

- URL: `https://localhost:7146/swagger`
- Hỗ trợ authentication với Bearer token
- Interactive API testing

### Postman Collection

Có thể import các endpoint vào Postman để test:

1. Export OpenAPI spec từ Swagger UI
2. Import vào Postman
3. Cấu hình Bearer token authentication

## Testing

### Unit Testing

- Sử dụng **xUnit** framework
- Test các service và business logic
- Mock dependencies với **Moq**

### Integration Testing

- Test các controller và API endpoints
- Test database interactions
- Test authentication và authorization

### Test Data

- Seed data cho development
- Factory patterns cho test data
- In-memory database cho testing

## Deployment

### Cấu hình Production

#### 1. Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="<production-connection-string>"
Jwt__Key="<secure-jwt-key>"
```

#### 2. Database Migration

```bash
dotnet ef database update --configuration Release
```

#### 3. Build và Deploy

```bash
dotnet publish -c Release -o ./publish
```

### Docker Support

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY publish/ .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "JobPortalApi.dll"]
```

### Cloud Deployment Options

- **Azure App Service**
- **AWS Elastic Beanstalk**
- **Google Cloud Run**
- **Docker containers**

## Kiến trúc và Design Patterns

### Layered Architecture (Kiến trúc phân tầng)

Dự án sử dụng kiến trúc phân tầng rõ ràng để tách biệt các concern:

- **Controllers**: API endpoints, routing, HTTP request/response handling
- **Services**: Business logic, validation, rules và workflow processing
- **Data Access**: Entity Framework Core DbContext cho database operations
- **DTOs**: Data transfer objects cho API input/output optimization
- **Models**: Domain entities và database schema definition

### Design Patterns được sử dụng

#### 1. **Dependency Injection Pattern**

```csharp
// Program.cs - DI Container Configuration
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();

// Controller - Constructor Injection
public class AdminUserController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUserController(IUserService userService)
    {
        _userService = userService;
    }
}
```

**Mục đích**: Giảm coupling, tăng testability, dễ dàng thay đổi implementation

#### 2. **Service Layer Pattern**

```csharp
// Interface definition
public interface IJobService
{
    Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId);
    Task<IEnumerable<JobPostDto>> GetAllAsync();
}

// Implementation
public class JobService : IJobService
{
    private readonly ApplicationDbContext _context;

    public JobService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId)
    {
        // Business logic implementation
    }
}
```

**Mục đích**: Tách biệt business logic khỏi controllers, tái sử dụng logic

#### 3. **Repository Pattern (Implicit với EF Core)**

```csharp
// DbContext acts as Unit of Work + Repository
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<JobPost> JobPosts { get; set; }
    public DbSet<Company> Companies { get; set; }
}

// Service sử dụng DbContext
public class JobService : IJobService
{
    private readonly ApplicationDbContext _context;

    public async Task<IEnumerable<JobPostDto>> GetAllAsync()
    {
        return await _context.JobPosts
            .Include(j => j.Company)
            .Include(j => j.Category)
            .Select(j => new JobPostDto { ... })
            .ToListAsync();
    }
}
```

**Mục đích**: Abstraction cho data access, centralized query logic

#### 4. **Data Transfer Object (DTO) Pattern**

```csharp
// Input DTO
public class CreateJobPostDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Salary { get; set; }
    // Chỉ các fields cần thiết cho creation
}

// Output DTO
public class JobPostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
    // Chỉ các fields cần thiết cho client
}
```

**Mục đích**: Tối ưu data transfer, security, versioning API

#### 5. **Factory Pattern (Entity Creation)**

```csharp
public async Task<JobPostDto> CreateAsync(CreateJobPostDto dto, Guid employerId)
{
    var job = new JobPost  // Factory-like creation
    {
        Id = Guid.NewGuid(),
        Title = dto.Title,
        Description = dto.Description,
        EmployerId = employerId,
        CreatedAt = DateTime.UtcNow
    };

    _context.JobPosts.Add(job);
    await _context.SaveChangesAsync();

    return await GetByIdAsync(job.Id);
}
```

**Mục đích**: Consistent object creation, encapsulation

#### 6. **Strategy Pattern (Authentication)**

```csharp
// Multiple authentication strategies
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT strategy */ });

// Role-based authorization strategies
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Recruiter")]
[Authorize(Roles = "Candidate")]
```

**Mục đích**: Flexible authentication/authorization strategies

#### 7. **Builder Pattern (Entity Framework Configuration)**

```csharp
// ApplicationDbContextModelSnapshot.cs
protected override void BuildModel(ModelBuilder modelBuilder)
{
    modelBuilder.Entity("JobPortalApi.Models.JobPost", b =>
    {
        b.Property<Guid>("Id")
            .ValueGeneratedOnAdd()
            .HasColumnType("uniqueidentifier");

        b.Property<string>("Title")
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        b.HasKey("Id");
        b.HasIndex("CategoryId");
        b.ToTable("JobPosts");
    });
}
```

**Mục đích**: Complex object configuration, fluent API

#### 8. **Observer Pattern (Entity Framework Change Tracking)**

```csharp
// EF Core automatically tracks entity changes
var user = await _context.Users.FindAsync(id);
user.FullName = "Updated Name";  // Change tracked
await _context.SaveChangesAsync();  // Observer notifies changes
```

**Mục đích**: Automatic change detection và persistence

#### 9. **Template Method Pattern (Base Controller)**

```csharp
// Implicit trong ASP.NET Core Controller lifecycle
// OnActionExecuting -> Action Method -> OnActionExecuted
[ApiController]
public class BaseController : ControllerBase
{
    // Template method pattern trong controller pipeline
}
```

**Mục đích**: Consistent request processing pipeline

#### 10. **Adapter Pattern (DTO Mapping)**

```csharp
private BlogDto MapToDto(Blog blog)
{
    return new BlogDto
    {
        Id = blog.Id,
        Title = blog.Title,
        Author = new BlogAuthorDto
        {
            Id = blog.Author.Id,
            Name = blog.Author.Name
        }
    };
}
```

**Mục đích**: Convert between incompatible interfaces (Entity ↔ DTO)

### SOLID Principles Implementation

#### **Single Responsibility Principle (SRP)**

- Mỗi service class có một trách nhiệm duy nhất (UserService chỉ handle User operations)
- Controllers chỉ handle HTTP concerns, không chứa business logic
- DTOs chỉ transfer data, không chứa behavior

#### **Open/Closed Principle (OCP)**

- Services implement interfaces, có thể extend mà không modify existing code
- New authentication strategies có thể thêm mà không change existing code

#### **Liskov Substitution Principle (LSP)**

- Mọi implementation của IUserService đều có thể thay thế cho nhau
- Interface contracts được tuân thủ nghiêm ngặt

#### **Interface Segregation Principle (ISP)**

- Tách biệt interfaces theo context: IUserService, IJobService, ICompanyService
- Admin và User services có interfaces riêng biệt

#### **Dependency Inversion Principle (DIP)**

- Controllers depend on abstractions (IUserService), không phụ thuộc concrete classes
- High-level modules không depend vào low-level modules

## Best Practices

### Code Quality

- Consistent naming conventions (PascalCase cho public, camelCase cho private)
- Comprehensive error handling
- Input validation
- Logging và monitoring
- Documentation comments (tiếng Việt)

### Performance

- Async/await patterns
- Database query optimization
- Proper indexing
- Caching strategies
- Pagination cho large datasets

### Security

- Input sanitization
- SQL injection prevention (EF Core)
- XSS protection
- Rate limiting
- Secure headers

## Troubleshooting

### Common Issues

#### 1. Database Connection Issues

```
Error: Cannot connect to SQL Server
```

**Solution**: Kiểm tra connection string và SQL Server service

#### 2. Migration Errors

```
Error: Pending model changes
```

**Solution**: Chạy `dotnet ef database update`

#### 3. JWT Token Issues

```
Error: 401 Unauthorized
```

**Solution**: Kiểm tra token format và expiration

#### 4. CORS Errors

```
Error: CORS policy blocked
```

**Solution**: Cấu hình CORS policy cho frontend domain

### Debugging Tips

- Sử dụng Visual Studio debugger
- Check logs trong Output window
- Sử dụng SQL Server Profiler cho database queries
- Test API endpoints với Swagger UI

## Contributing

### Development Workflow

1. Fork repository
2. Create feature branch
3. Write tests cho new features
4. Ensure all tests pass
5. Submit pull request

### Code Standards

- Tuân thủ C# coding conventions
- Comment bằng tiếng Việt cho business logic
- Unit tests cho mọi service method
- Integration tests cho API endpoints

### Commit Message Format

```
feat: thêm API tìm kiếm ứng viên cho recruiter
fix: sửa lỗi upload CV không thành công
docs: cập nhật README.md
test: thêm unit tests cho JobService
```

## Changelog

### Version 1.0.0 (Current)

- ✅ User authentication và authorization
- ✅ Company management
- ✅ Job posting và application
- ✅ Candidate profile management
- ✅ Admin dashboard
- ✅ File upload cho CV
- ✅ Search và filtering
- ✅ Notification system
- ✅ Review system
- ✅ Blog system

### Planned Features (Future Versions)

- 📧 Email notifications
- 🔍 Advanced search với Elasticsearch
- 📊 Advanced analytics và reporting
- 💬 Real-time messaging
- 📱 Mobile app support
- 🌍 Multi-language support
- 🎯 AI-powered job matching

## Support và Contact

### Documentation

- **API Docs**: `/swagger` endpoint
- **GitHub Wiki**: Additional documentation
- **Code Comments**: Inline documentation

### Community

- **Issues**: GitHub Issues cho bug reports
- **Discussions**: GitHub Discussions cho Q&A
- **Pull Requests**: Contributions welcome

### Technical Support

- **Email**: [support@jobportal.com]
- **Discord**: [Community Discord Server]
- **Stack Overflow**: Tag `jobportal-api`

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Phiên bản README**: 1.0.0  
**Cập nhật lần cuối**: Tháng 7, 2025
