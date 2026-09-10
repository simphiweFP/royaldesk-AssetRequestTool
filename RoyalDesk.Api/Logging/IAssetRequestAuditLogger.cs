using RoyalDesk.Api.DTOs;

namespace RoyalDesk.Api.Logging;

public interface IAssetRequestAuditLogger
{
    Task LogCreatedAsync(
        AssetRequestResponseDto request,
        CancellationToken cancellationToken = default);
}
