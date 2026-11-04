using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Email;
using RoyalDesk.Api.HelpDesk;
using RoyalDesk.Api.Logging;
using RoyalDesk.Api.Models;
using RoyalDesk.Api.Repositories;

namespace RoyalDesk.Api.Services;

public sealed class AssetRequestService(
    IAssetRequestRepository repository,
    IAssetRequestAuditLogger auditLogger,
    TicketNotificationService? notifications = null) : IAssetRequestService
{
    public async Task<AssetRequestResponseDto> CreateAsync(
        CreateAssetRequestDto request,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var requesterEmail = string.IsNullOrWhiteSpace(request.RequesterEmail)
            ? null
            : request.RequesterEmail.Trim();
        var trackingPin = requesterEmail is null ? TrackingPinSecurity.GeneratePin() : null;

        var requestType = string.IsNullOrWhiteSpace(request.RequestType) ? "Equipment" : request.RequestType.Trim();
        var entity = new AssetRequest
        {
            RequestType = requestType,
            RequesterName = request.RequesterName.Trim(),
            RequesterEmail = requesterEmail,
            TrackingPinHash = trackingPin is null ? null : TrackingPinSecurity.Hash(trackingPin),
            Branch = request.Branch.Trim(),
            Department = request.Department.Trim(),
            ItemType = request.ItemType.Trim(),
            Quantity = requestType.Equals("Equipment", StringComparison.OrdinalIgnoreCase) ? request.Quantity : 1,
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
            RequestType = entity.RequestType,
            RequesterName = entity.RequesterName,
            RequesterEmail = entity.RequesterEmail,
            TrackingPin = trackingPin,
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
        if (notifications is not null) await notifications.TicketCreatedAsync(
            entity.Id,
            response.TicketNumber,
            entity.RequesterName,
            entity.RequesterEmail,
            entity.Branch,
            entity.Department,
            entity.ItemType,
            entity.Reason,
            trackingPin,
            cancellationToken);

        return response;
    }
}
