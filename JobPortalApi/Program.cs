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
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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
builder.Services.AddScoped<IRecruiterCandidateService, RecruiterCandidateService>();
builder.Services.AddScoped<ISavedJobService, SavedJobService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.User.IReviewService, JobPortalApi.Services.User.ReviewService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<JobPortalApi.Services.Interface.User.ICompanyService, JobPortalApi.Services.User.CompanyService>();
builder.Services.AddScoped<IJobService, JobService>();

builder.Services.AddScoped<IApplyService, ApplyService>();
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
        });
    builder.Services.AddAuthorization();

    builder.Services.AddControllers();
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
// Hãy bật HTTPS nếu bạn dùng Swagger
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
        RequestPath = ""
    });
    app.UseCors("AllowFrontends"); // phải gọi trước UseAuthorization() 
    app.UseHttpsRedirection();
    app.UseAuthentication(); // 🛡 Bắt buộc đặt trước UseAuthorization

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
    Console.WriteLine("Environment: " + app.Environment.EnvironmentName);



