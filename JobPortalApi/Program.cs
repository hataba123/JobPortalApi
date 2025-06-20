using JobPortalApi.Services.Interface.User;
using JobPortalApi.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Add services to the container.

// dang ky AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// dang ky JobPostService
builder.Services.AddScoped<IJobService, JobService>();
// dang ky ApplyService
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

    app.UseCors("AllowFrontends"); // phải gọi trước UseAuthorization() 
    app.UseHttpsRedirection();
    app.UseAuthentication(); // 🛡 Bắt buộc đặt trước UseAuthorization

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
    Console.WriteLine("Environment: " + app.Environment.EnvironmentName);



