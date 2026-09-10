using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Logging;
using RoyalDesk.Api.Models;
using RoyalDesk.Api.Repositories;
using RoyalDesk.Api.Services;

namespace RoyalDesk.Api.Tests;

public sealed class AssetRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_SavesAuthenticatedUserAndLogsResponse()
    {
        var repository = new RecordingRepository();
        var auditLogger = new RecordingAuditLogger();
        var service = new AssetRequestService(repository, auditLogger);

        var response = await service.CreateAsync(new CreateAssetRequestDto
        {
            Branch = "Phoenix",
            Department = "IT",
            ItemType = "Laptop",
            Quantity = 1,
            Reason = "Laptop required for a new starter."
        }, "simphiwe");

        Assert.Equal(42, response.Id);
        Assert.Equal("simphiwe", repository.Saved?.RequestedBy);
        Assert.Equal(42, auditLogger.Logged?.Id);
        Assert.Equal("simphiwe", auditLogger.Logged?.RequestedBy);
    }

    private sealed class RecordingRepository : IAssetRequestRepository
    {
        public AssetRequest? Saved { get; private set; }

        public Task<int> CreateAsync(
            AssetRequest request,
            CancellationToken cancellationToken = default)
        {
            Saved = request;
            return Task.FromResult(42);
        }
    }

    private sealed class RecordingAuditLogger : IAssetRequestAuditLogger
    {
        public AssetRequestResponseDto? Logged { get; private set; }

        public Task LogCreatedAsync(
            AssetRequestResponseDto request,
            CancellationToken cancellationToken = default)
        {
            Logged = request;
            return Task.CompletedTask;
        }
    }
}
