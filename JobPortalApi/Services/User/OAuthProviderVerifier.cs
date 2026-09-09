using System.Net.Http.Headers;
using System.Text.Json;

namespace JobPortalApi.Services.User;

/// Xác minh access token với OAuth provider trước khi tạo/liên kết tài khoản.
public sealed class OAuthProviderVerifier
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OAuthProviderVerifier(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OAuthIdentity> VerifyAsync(string provider, string accessToken)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var token = accessToken.Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("OAuth token trống.");

        var endpoint = normalizedProvider switch
        {
            "google" => "https://openidconnect.googleapis.com/v1/userinfo",
            "facebook" => "https://graph.facebook.com/me?fields=id,name,email",
            "github" => "https://api.github.com/user",
            _ => null
        };
        if (endpoint == null)
            throw new UnauthorizedAccessException("OAuth provider không được hỗ trợ.");

        using var profile = await GetJsonAsync(endpoint, token);
        var providerAccountId = ReadString(profile.RootElement, "sub", "id");
        var email = ReadString(profile.RootElement, "email");
        var name = ReadString(profile.RootElement, "name", "login");

        if (normalizedProvider == "google" &&
            profile.RootElement.TryGetProperty("email_verified", out var verified) &&
            verified.ValueKind == JsonValueKind.False)
        {
            throw new UnauthorizedAccessException("Email Google chưa được xác minh.");
        }

        if (normalizedProvider == "github" && email == null)
        {
            using var emails = await GetJsonAsync("https://api.github.com/user/emails", token);
            if (emails.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in emails.RootElement.EnumerateArray())
                {
                    var isVerified = item.TryGetProperty("verified", out var itemVerified) &&
                        itemVerified.ValueKind == JsonValueKind.True;
                    var isPrimary = item.TryGetProperty("primary", out var itemPrimary) &&
                        itemPrimary.ValueKind == JsonValueKind.True;
                    var itemEmail = ReadString(item, "email");
                    if (isVerified && isPrimary && itemEmail != null)
                    {
                        email = itemEmail;
                        break;
                    }
                }
            }
        }

        if (providerAccountId == null || email == null)
            throw new UnauthorizedAccessException("OAuth provider không trả về danh tính hợp lệ.");

        return new OAuthIdentity(
            normalizedProvider,
            providerAccountId,
            email.ToLowerInvariant(),
            name ?? email.Split('@')[0]);
    }

    private async Task<JsonDocument> GetJsonAsync(string endpoint, string accessToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("oauth-provider");
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.UserAgent.ParseAdd("JobPortal/1.0");

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("OAuth provider rejected token.");

            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonDocument.ParseAsync(stream);
        }
        catch
        {
            throw new UnauthorizedAccessException("Không thể xác minh OAuth token.");
        }
    }

    private static string? ReadString(JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (value.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                var text = property.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }
        return null;
    }
}

public sealed record OAuthIdentity(
    string Provider,
    string ProviderAccountId,
    string Email,
    string Name);
