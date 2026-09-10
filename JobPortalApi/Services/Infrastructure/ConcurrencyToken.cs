namespace JobPortalApi.Services.Infrastructure;

public static class ConcurrencyToken
{
    public static string Encode(byte[]? value) =>
        Convert.ToBase64String(value ?? Array.Empty<byte>());

    public static byte[] Decode(string value)
    {
        try
        {
            return Convert.FromBase64String(value.Trim('"'));
        }
        catch (FormatException)
        {
            throw new ArgumentException("Concurrency token không hợp lệ.");
        }
    }
}
