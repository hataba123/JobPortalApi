namespace JobPortalApi.DTOs.Shared
{
    public class OAuthLoginRequest
    {
        public string Provider { get; set; }           // "google", "github", ...
        public string AccessToken { get; set; }
        public string? IdToken { get; set; }
    }
}
