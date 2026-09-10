using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Logging;

namespace RoyalDesk.Api.Tests;

public sealed class FileAssetRequestAuditLoggerTests : IDisposable
{
    private readonly string _logPath = Path.Combine(
        Path.GetTempPath(),
        $"royaldesk-audit-{Guid.NewGuid():N}.log");

    [Fact]
    public async Task LogCreatedAsync_AppendsRequestDetails()
    {
        var logger = new FileAssetRequestAuditLogger(_logPath);

        await logger.LogCreatedAsync(new AssetRequestResponseDto
        {
            Id = 21,
            Branch = "Phoenix",
            Department = "IT",
            ItemType = "Laptop",
            Quantity = 1,
            RequestedBy = "simphiwe",
            RequestedAtUtc = DateTime.UtcNow
        });

        var contents = await File.ReadAllTextAsync(_logPath);

        Assert.Contains("AssetRequestCreated", contents);
        Assert.Contains("simphiwe", contents);
        Assert.Contains("Laptop", contents);
    }

    public void Dispose()
    {
        if (File.Exists(_logPath))
            File.Delete(_logPath);
    }
}
