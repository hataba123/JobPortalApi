using System.Text;

namespace JobPortalApi.Services.Helpers;

public static class PrivateCvStorage
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    public static string RootDirectory => Path.Combine(Directory.GetCurrentDirectory(), "private-data", "cv");

    public static void ValidatePdf(string fileName, long fileLength, byte[] content)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CV chỉ chấp nhận định dạng PDF.");
        if (fileLength <= 0 || fileLength > MaxFileSize)
            throw new ArgumentException("File CV vượt quá dung lượng cho phép.");
        if (content.Length < 5 || Encoding.ASCII.GetString(content, 0, 5) != "%PDF-")
            throw new ArgumentException("Nội dung file không phải PDF hợp lệ.");
    }

    public static string? Resolve(string? storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            !Guid.TryParse(Path.GetFileNameWithoutExtension(storageKey), out _) ||
            !string.Equals(Path.GetExtension(storageKey), ".pdf", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(storageKey) != storageKey)
            return null;

        var root = Path.GetFullPath(RootDirectory);
        var path = Path.GetFullPath(Path.Combine(root, storageKey));
        return path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? path
            : null;
    }
}
