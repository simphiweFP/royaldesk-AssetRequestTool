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
            RequesterName = request.RequesterName.Trim(),
            RequesterEmail = string.IsNullOrWhiteSpace(request.RequesterEmail)
                ? null
                : request.RequesterEmail.Trim(),
            Branch = request.Branch.Trim(),
            Department = request.Department.Trim(),
            ItemType = request.ItemType.Trim(),
            Quantity = request.Quantity,
            Reason = request.Reason.Trim(),
            Status = "New",
            RequestedBy = requestedBy,
            RequestedAtUtc = DateTime.UtcNow
        };

        entity.Id = await repository.CreateAsync(entity, cancellationToken);

        var response = new AssetRequestResponseDto
        {
            Id = entity.Id,
            TicketNumber = entity.TicketNumber ?? $"RT-IT-{entity.Id:000000}",
            RequesterName = entity.RequesterName,
            RequesterEmail = entity.RequesterEmail,
            Branch = entity.Branch,
            Department = entity.Department,
            ItemType = entity.ItemType,
            Quantity = entity.Quantity,
            Reason = entity.Reason,
            Status = entity.Status,
            RequestedBy = entity.RequestedBy,
            RequestedAtUtc = entity.RequestedAtUtc
        };

        await auditLogger.LogCreatedAsync(response, cancellationToken);
        return response;
    }
}
