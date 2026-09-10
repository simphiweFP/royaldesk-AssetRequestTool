using RoyalDesk.Api.DTOs;

namespace RoyalDesk.Api.Services;

public interface IAssetRequestService
{
    Task<AssetRequestResponseDto> CreateAsync(
        CreateAssetRequestDto request,
        string requestedBy,
        CancellationToken cancellationToken = default);
}
