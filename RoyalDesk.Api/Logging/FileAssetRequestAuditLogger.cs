using System.Text;
using System.Text.Json;
using RoyalDesk.Api.DTOs;

namespace RoyalDesk.Api.Logging;

public sealed class FileAssetRequestAuditLogger : IAssetRequestAuditLogger
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FileAssetRequestAuditLogger(IHostEnvironment environment, IConfiguration configuration)
        : this(Path.Combine(
            environment.ContentRootPath,
            configuration["AuditLogging:FilePath"] ?? "logs/asset-requests.log"))
    {
    }

    public FileAssetRequestAuditLogger(string filePath)
    {
        _filePath = filePath;
    }

    public async Task LogCreatedAsync(
        AssetRequestResponseDto request,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(new
        {
            Event = "AssetRequestCreated",
            request.Id,
            request.RequestedAtUtc,
            request.RequestedBy,
            request.Branch,
            request.Department,
            request.ItemType,
            request.Quantity
        }) + Environment.NewLine;

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_filePath, line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
