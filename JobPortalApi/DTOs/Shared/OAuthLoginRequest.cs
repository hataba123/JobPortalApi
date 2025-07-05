namespace JobPortalApi.DTOs.Shared
{
    public class OAuthLoginRequest
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }           // "google", "github", ...
        public string ProviderAccountId { get; set; }  // ID từ phía provider
    }
}
