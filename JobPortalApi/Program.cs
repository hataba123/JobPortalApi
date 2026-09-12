using JobPortalApi.Models;
using JobPortalApi.Services.Admin;
using JobPortalApi.Services.Interface.Admin;
using JobPortalApi.Services.Interface.User;
using JobPortalApi.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Claims;
using System.Text;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using JobPortalApi.Services.Matching;
using JobPortalApi.Services.Payments;
using JobPortalApi.Services.Notifications;
using JobPortalApi.Services.Infrastructure;
using JobPortalApi.Middleware;
using JobPortalApi.Services.Media;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    // Giới hạn mặc định cho mọi request, kể cả khi request đi thẳng vào API
    // thay vì qua BFF/Nginx.
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32 ||
    string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Jwt:Key tối thiểu 32 ký tự, Jwt:Issuer và Jwt:Audience là bắt buộc.");

var backgroundJobsEnabled = builder.Configuration.GetValue("BackgroundJobs:Enabled", true);
var emailWebhookUrl = builder.Configuration["Email:WebhookUrl"]
    ?? Environment.GetEnvironmentVariable("EMAIL_WEBHOOK_URL");
if (builder.Environment.IsProduction() && backgroundJobsEnabled && string.IsNullOrWhiteSpace(emailWebhookUrl))
    throw new InvalidOperationException("Email:WebhookUrl là bắt buộc khi worker production được bật.");

var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("JobPortalApi");
var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyRingPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    Directory.CreateDirectory(dataProtectionKeyPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));
}

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
builder.Services.AddHttpClient("email-provider", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddScoped<OAuthProviderVerifier>();
builder.Services.AddScoped<IRecruiterCandidateService, RecruiterCandidateService>();
builder.Services.AddScoped<ISavedJobService, SavedJobService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.User.IReviewService, JobPortalApi.Services.User.ReviewService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.User.ICompanyService, JobPortalApi.Services.User.CompanyService>();
builder.Services.AddScoped<IJobService, JobService>();

builder.Services.AddScoped<IApplyService, ApplyService>();
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<IJobReportService, JobReportService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<CreditLedgerService>();
builder.Services.AddScoped<PublicMediaService>();
builder.Services.AddSingleton<IClamAvScanner, ClamAvScanner>();
builder.Services.AddHostedService<BackgroundProcessingService>();
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.ConnectTimeout = 1000;
        redisOptions.SyncTimeout = 1000;
        return ConnectionMultiplexer.Connect(redisOptions);
    });
}
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
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000", "http://localhost:3001", "http://localhost:3002"];
    options.AddPolicy("AllowFrontends",
        corsBuilder =>
        {
            corsBuilder.WithOrigins(allowedOrigins)
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
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
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
    builder.Services.AddAuthorization(options =>
    {
        // Deny by default; every public endpoint opts in explicitly with
        // [AllowAnonymous]. This prevents a newly-added mutation from being
        // accidentally exposed without authentication.
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
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

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(item => item.Value?.Errors.Count > 0)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());
            return new BadRequestObjectResult(new
            {
                statusCode = StatusCodes.Status400BadRequest,
                code = "VALIDATION_ERROR",
                message = "Dữ liệu không hợp lệ.",
                traceId = context.HttpContext.TraceIdentifier,
                details = errors
            });
        };
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

var knownProxyAddresses = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
var knownProxyNetworks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [];
if (knownProxyAddresses.Length > 0 || knownProxyNetworks.Length > 0)
{
    var forwardedHeaders = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 2
    };
    foreach (var address in knownProxyAddresses)
        if (System.Net.IPAddress.TryParse(address, out var parsedAddress))
            forwardedHeaders.KnownProxies.Add(parsedAddress);
    foreach (var network in knownProxyNetworks)
    {
        var parts = network.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            System.Net.IPAddress.TryParse(parts[0], out var prefix) &&
            int.TryParse(parts[1], out var prefixLength) &&
            prefixLength >= 0 && prefixLength <= (prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))
        {
            forwardedHeaders.KnownNetworks.Add(new IPNetwork(prefix, prefixLength));
        }
    }
    app.UseForwardedHeaders(forwardedHeaders);
}

if (builder.Configuration.GetValue<bool>("Seed:Enabled") ||
    string.Equals(Environment.GetEnvironmentVariable("SEED_DATABASE"), "true", StringComparison.OrdinalIgnoreCase))
{
    using var seedScope = app.Services.CreateScope();
    await DatabaseSeeder.SeedAsync(seedScope.ServiceProvider, builder.Configuration);
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();


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
    var webRoot = app.Environment.WebRootPath
        ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(webRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(webRoot),
        RequestPath = "",
        OnPrepareResponse = static context =>
        {
            var physicalPath = context.File.PhysicalPath;
            if (!string.IsNullOrWhiteSpace(physicalPath) &&
                string.Equals(Path.GetExtension(physicalPath), ".svg", StringComparison.OrdinalIgnoreCase))
            {
                // SVG cũ có thể vẫn còn trong volume. Vô hiệu hóa script và
                // tài nguyên ngoài trong lúc chưa chuyển sang media domain riêng.
                context.Context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
                context.Context.Response.Headers["Content-Disposition"] = "attachment";
            }
        }
    });
    app.UseCors("AllowFrontends"); // phải gọi trước UseAuthorization() 
    app.UseHttpsRedirection();
    app.UseAuthentication(); // 🛡 Bắt buộc đặt trước UseAuthorization
    app.UseMiddleware<RedisRateLimitMiddleware>();
    app.UseRateLimiter();

    app.UseAuthorization();

    app.MapControllers();

    var environmentName = app.Environment.EnvironmentName;
    app.Run();
    Console.WriteLine("Environment: " + environmentName);

    static string GetClientAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

public partial class Program
{
}


