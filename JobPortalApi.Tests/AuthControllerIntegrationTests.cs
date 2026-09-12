using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using JobPortalApi.DTOs.Shared;
using JobPortalApi.DTOs.shared;
using JobPortalApi.Models;
using JobPortalApi.Services.Interface.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace JobPortalApi.Tests;

public sealed class AuthControllerIntegrationTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthControllerIntegrationTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "old-password",
                NewPassword = "new-password"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithAuthentication_ReturnsTheCurrentUser()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });
        Assert.Equal(AuthApiFactory.TestEmail, user?.Email);
    }

    [Fact]
    public async Task ChangePassword_WithAuthentication_CallsTheProtectedWorkflow()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordRequest
            {
                CurrentPassword = "old-password",
                NewPassword = "new-password"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.AuthService.ChangePasswordCalls > 0);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.TestScheme);
        return client;
    }
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string TestEmail = "integration-test@example.com";

    public FakeAuthService AuthService { get; } = new();

    public AuthApiFactory()
    {
        // WebApplication.CreateBuilder reads these values before ConfigureTestServices runs.
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-signing-key-that-is-long-enough");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "JobPortalAPI");
        Environment.SetEnvironmentVariable("Jwt__Audience", "JobPortalClient");
        Environment.SetEnvironmentVariable("BackgroundJobs__Enabled", "false");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=(local);Database=JobPortalIntegration;Trusted_Connection=True;");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-test-signing-key-that-is-long-enough",
                ["Jwt:Issuer"] = "JobPortalAPI",
                ["Jwt:Audience"] = "JobPortalClient",
                ["BackgroundJobs:Enabled"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=JobPortalIntegration;Trusted_Connection=True;"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(AuthService);

            services.AddAuthentication(options =>
            {
            options.DefaultAuthenticateScheme = TestAuthenticationHandler.TestScheme;
            options.DefaultChallengeScheme = TestAuthenticationHandler.TestScheme;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.TestScheme,
                _ => { });
        });
    }
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestScheme = "IntegrationTest";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization) ||
            !string.Equals(authorization.ToString(), TestScheme, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Parse("11111111-1111-1111-1111-111111111111").ToString()),
            new Claim(ClaimTypes.Email, AuthApiFactory.TestEmail)
        };
        var identity = new ClaimsIdentity(claims, TestScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class FakeAuthService : IAuthService
{
    public int ChangePasswordCalls { get; private set; }

    public Task<string> RegisterAsync(RegisterRequest request) =>
        Task.FromResult("integration-token");

    public Task<string> LoginAsync(LoginRequest request) =>
        Task.FromResult("integration-token");

    public Task<UserDto> GetUserByEmailAsync(string email) =>
        Task.FromResult(new UserDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = email,
            FullName = "Integration User",
            Role = UserRole.Candidate
        });

    public Task<AuthResponse> OAuthLoginAsync(OAuthLoginRequest request, string exchangeSecret) =>
        Task.FromResult(new AuthResponse
        {
            Token = "integration-token",
            User = new UserDto
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = AuthApiFactory.TestEmail,
                FullName = "Integration User",
                Role = UserRole.Candidate
            }
        });

    public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        ChangePasswordCalls++;
        return Task.CompletedTask;
    }

    public Task CreatePasswordResetRequestAsync(string email) => Task.CompletedTask;

    public Task ResetPasswordAsync(ResetPasswordRequest request) => Task.CompletedTask;
}
