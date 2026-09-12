using Microsoft.AspNetCore.Http;

namespace JobPortalApi.Services.Media;

public class PublicMediaService
{
    private const long MaxImageSize = 5 * 1024 * 1024;
    private readonly IWebHostEnvironment _environment;

    public PublicMediaService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveImageAsync(IFormFile file, string directory, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0 || file.Length > MaxImageSize)
            throw new ArgumentException("Ảnh phải lớn hơn 0 và không vượt quá 5MB.");

        var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        await using var input = file.OpenReadStream();
        await using var memory = new MemoryStream();
        await input.CopyToAsync(memory, cancellationToken);
        var content = memory.ToArray();
        var extension = DetectExtension(contentType, content);
        if (extension == null)
            throw new ArgumentException("Ảnh không đúng định dạng hoặc chữ ký file.");

        var root = Path.GetFullPath(Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            "uploads", directory));
        Directory.CreateDirectory(root);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var target = Path.GetFullPath(Path.Combine(root, fileName));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Đường dẫn ảnh không hợp lệ.");

        await System.IO.File.WriteAllBytesAsync(target, content, cancellationToken);
        return $"/uploads/{directory}/{fileName}";
    }

    private static string? DetectExtension(string contentType, byte[] content)
    {
        if (contentType == "image/png" && content.Length >= 8 &&
            content.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return ".png";
        if ((contentType == "image/jpeg" || contentType == "image/jpg") && content.Length >= 3 &&
            content.AsSpan(0, 3).SequenceEqual(new byte[] { 255, 216, 255 }))
            return ".jpg";
        return null;
    }
}
