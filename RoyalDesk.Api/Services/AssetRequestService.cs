using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Logging;
using RoyalDesk.Api.Models;
using RoyalDesk.Api.Repositories;

namespace RoyalDesk.Api.Services;

public sealed class AssetRequestService(
    IAssetRequestRepository repository,
    IAssetRequestAuditLogger auditLogger) : IAssetRequestService
{
    public async Task<AssetRequestResponseDto> CreateAsync(
        CreateAssetRequestDto request,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var entity = new AssetRequest
        {
            Branch = request.Branch.Trim(),
            Department = request.Department.Trim(),
            ItemType = request.ItemType.Trim(),
            Quantity = request.Quantity,
            Reason = request.Reason.Trim(),
            RequestedBy = requestedBy,
            RequestedAtUtc = DateTime.UtcNow
        };

        entity.Id = await repository.CreateAsync(entity, cancellationToken);

        var response = new AssetRequestResponseDto
        {
            Id = entity.Id,
            Branch = entity.Branch,
            Department = entity.Department,
            ItemType = entity.ItemType,
            Quantity = entity.Quantity,
            Reason = entity.Reason,
            RequestedBy = entity.RequestedBy,
            RequestedAtUtc = entity.RequestedAtUtc
        };

        await auditLogger.LogCreatedAsync(response, cancellationToken);
        return response;
    }
}
