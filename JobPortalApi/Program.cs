using JobPortalApi.Models;
using JobPortalApi.Services.Admin;
using JobPortalApi.Services.Interface.Admin;
using JobPortalApi.Services.Interface.User;
using JobPortalApi.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using JobPortalApi.Services.Matching;
using JobPortalApi.Services.Payments;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException("Jwt:Key và Jwt:Issuer là bắt buộc.");

// Add services to the container.
// Admin services
builder.Services.AddScoped<JobPortalApi.Services.Interface.Admin.ICompanyService, JobPortalApi.Services.Admin.CompanyService>();
builder.Services.AddScoped<IBlogService, BlogService>(); // 👈 THÊM DÒNG NÀY
builder.Services.AddScoped<IRecruiterDashboardService, RecruiterDashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJobPostService, JobPostService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.Admin.IReviewService, JobPortalApi.Services.Admin.ReviewService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

//user service
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient("oauth-provider", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<OAuthProviderVerifier>();
builder.Services.AddScoped<IRecruiterCandidateService, RecruiterCandidateService>();
builder.Services.AddScoped<ISavedJobService, SavedJobService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.User.IReviewService, JobPortalApi.Services.User.ReviewService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.User.ICompanyService, JobPortalApi.Services.User.CompanyService>();
builder.Services.AddScoped<IJobService, JobService>();

builder.Services.AddScoped<IApplyService, ApplyService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<PaymentService>();
// add db context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 6,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
));
// Cách 1: Cấu hình Swagger để phân biệt schema theo namespace
//  SwaggerGeneratorException: Failed to generate schema for type CreateJobPostDto
// 👉 Vì hai DTO khác nhau (User.JobPost.CreateJobPostDto và AdminJobPost.CreateJobPostDto) cùng tên class, nên Swashbuckle không thể phân biệt được khi sinh schema Swagger → gây lỗi trùng schemaId.
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName); // 👈 Quan trọng
});
// Bật và cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002") // Port của Next.js
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
});

// add jwt authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Config xác thực JWT
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                // 🔥 Quan trọng: map đúng claim role
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var versionValue = context.Principal?.FindFirst("pwd_ver")?.Value;
                    // Token phát hành trước migration không có version; cho phép đến khi hết hạn.
                    if (versionValue == null) return;
                    if (!Guid.TryParse(userIdValue, out var userId) ||
                        !int.TryParse(versionValue, out var tokenVersion))
                    {
                        context.Fail("Token không hợp lệ.");
                        return;
                    }

                    var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var currentVersion = await db.Users
                        .Where(u => u.Id == userId)
                        .Select(u => (int?)u.PasswordVersion)
                        .SingleOrDefaultAsync();
                    if (currentVersion == null || currentVersion.Value != tokenVersion)
                        context.Fail("Phiên đăng nhập đã hết hạn.");
                }
            };
        });
    builder.Services.AddAuthorization();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            GetClientAddress(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
        options.AddPolicy("payment", context => RateLimitPartition.GetFixedWindowLimiter(
            GetClientAddress(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            In = ParameterLocation.Header,
            Description = "Phải nhập 'Bearer' đằng trước."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer" }
            },
            new string[] { }
        }
        });
    });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.Use(async (context, next) =>
    {
        const string headerName = "X-Correlation-ID";
        var supplied = context.Request.Headers[headerName].FirstOrDefault();
        var correlationId = Guid.TryParse(supplied, out var parsed)
            ? parsed.ToString("D")
            : Guid.NewGuid().ToString("D");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[headerName] = correlationId;

        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RequestPipeline");
        var started = Stopwatch.GetTimestamp();
        logger.LogInformation("Request started {Method} {Path} {CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);
        try
        {
            await next();
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            logger.LogInformation("Request completed {StatusCode} {ElapsedMs} {CorrelationId}",
                context.Response.StatusCode, elapsed, correlationId);
        }
    });
// Hãy bật HTTPS nếu bạn dùng Swagger
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/uploads/cv"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        await next();
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
        RequestPath = ""
    });
    app.UseCors("AllowFrontends"); // phải gọi trước UseAuthorization() 
    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication(); // 🛡 Bắt buộc đặt trước UseAuthorization

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
    Console.WriteLine("Environment: " + app.Environment.EnvironmentName);

    static string GetClientAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";



