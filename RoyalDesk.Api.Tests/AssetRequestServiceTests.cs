using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Models;
using RoyalDesk.Api.Repositories;
using RoyalDesk.Api.Services;

namespace RoyalDesk.Api.Tests;

public sealed class AssetRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_SavesAuthenticatedUserAndReturnsResponse()
    {
        var repository = new RecordingRepository();
        var service = new AssetRequestService(repository);

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
}
