using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace JobPortalApi.Services.Media;

public interface IClamAvScanner
{
    Task ScanAsync(string filePath, CancellationToken cancellationToken = default);
}

public sealed class ClamAvScanner : IClamAvScanner
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClamAvScanner> _logger;

    public ClamAvScanner(IConfiguration configuration, ILogger<ClamAvScanner> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ScanAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!_configuration.GetValue("ClamAv:Enabled", false))
            return;

        var host = _configuration["ClamAv:Host"] ?? "clamav";
        var port = _configuration.GetValue("ClamAv:Port", 3310);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_configuration.GetValue("ClamAv:TimeoutSeconds", 10)));
        var scanToken = timeout.Token;
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, scanToken);
        await using var stream = client.GetStream();

        await stream.WriteAsync(Encoding.ASCII.GetBytes("INSTREAM\0"), scanToken);
        await using (var input = File.OpenRead(filePath))
        {
            var buffer = new byte[64 * 1024];
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(), scanToken)) > 0)
            {
                var length = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(length, (uint)read);
                await stream.WriteAsync(length, scanToken);
                await stream.WriteAsync(buffer.AsMemory(0, read), scanToken);
            }
        }

        await stream.WriteAsync(new byte[4], scanToken);
        await stream.FlushAsync(scanToken);

        var response = new byte[1024];
        var bytesRead = await stream.ReadAsync(response.AsMemory(), scanToken);
        var result = Encoding.UTF8.GetString(response, 0, bytesRead).Trim();
        if (result.EndsWith("OK", StringComparison.OrdinalIgnoreCase))
            return;

        _logger.LogWarning("ClamAV từ chối file CV với kết quả {ScanResult}.", result);
        throw new InvalidDataException("File CV không vượt qua kiểm tra an toàn.");
    }
}
