using RoyalDesk.Api.Models;

namespace RoyalDesk.Api.Repositories;

public interface IAssetRequestRepository
{
    Task<int> CreateAsync(AssetRequest request, CancellationToken cancellationToken = default);
}
