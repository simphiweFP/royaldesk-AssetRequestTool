using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.HelpDesk;

public sealed class HelpDeskRepository(IDbConnectionFactory connectionFactory) : IHelpDeskRepository
{
    private sealed class TicketOwnerRow
    {
        public string Status { get; set; } = string.Empty;
        public int? AssignedToId { get; set; }
    }

    public async Task<IReadOnlyList<HelpDeskTicketDto>> GetTicketsAsync(string? status, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TicketNumber, RequestType, RequesterName, RequesterEmail, Branch, Department,
                   ItemType, Quantity, Reason, Status, AssignedToId, AssignedToName,
                   AssignedAtUtc, StartedAtUtc, ResolvedAtUtc, RequestedAtUtc, LastUpdatedAtUtc
            FROM AssetRequests
            WHERE (@Status IS NULL OR @Status = '' OR Status = @Status)
            ORDER BY CASE WHEN Status = 'New' THEN 0 WHEN Status = 'Assigned' THEN 1 WHEN Status = 'In Progress' THEN 2 ELSE 3 END,
                     RequestedAtUtc DESC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<HelpDeskTicketDto>(new CommandDefinition(sql, new { Status = status }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<HelpDeskTicketDto?> GetTicketAsync(int id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TicketNumber, RequestType, RequesterName, RequesterEmail, Branch, Department,
                   ItemType, Quantity, Reason, Status, AssignedToId, AssignedToName,
                   AssignedAtUtc, StartedAtUtc, ResolvedAtUtc, RequestedAtUtc, LastUpdatedAtUtc
            FROM AssetRequests WHERE Id = @Id;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<HelpDeskTicketDto>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<TicketStatusHistoryDto>> GetHistoryAsync(int ticketId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc
            FROM TicketStatusHistory
            WHERE TicketId = @TicketId
            ORDER BY ChangedAtUtc ASC, Id ASC;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<TicketStatusHistoryDto>(new CommandDefinition(sql, new { TicketId = ticketId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ITTeamMemberDto>> GetTeamAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, DisplayName, Email, Username, Role, IsActive
            FROM ITTeamMembers
            ORDER BY IsActive DESC, DisplayName;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = await connection.QueryAsync<ITTeamMemberDto>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<ITTeamMemberDto?> GetActiveTeamMemberByUsernameAsync(string username, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, DisplayName, Email, Username, Role, IsActive
            FROM ITTeamMembers
            WHERE (lower(Username) = lower(@Username) OR lower(DisplayName) = lower(@Username))
              AND IsActive = 1
            LIMIT 1;
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ITTeamMemberDto>(new CommandDefinition(sql, new { Username = username }, cancellationToken: ct));
    }

    public async Task<ITTeamMemberDto> CreateTeamMemberAsync(CreateITTeamMemberDto dto, CancellationToken ct = default)
    {
        const string insert = """
            INSERT INTO ITTeamMembers (DisplayName, Email, Username, Role, IsActive, CreatedAtUtc)
            VALUES (@DisplayName, @Email, @Username, @Role, 1, @CreatedAtUtc);
            SELECT last_insert_rowid();
            """;

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition(insert, new
        {
            DisplayName = dto.DisplayName.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Username = dto.Username.Trim(),
            Role = string.IsNullOrWhiteSpace(dto.Role) ? "IT Technician" : dto.Role.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken: ct));

        return await connection.QuerySingleAsync<ITTeamMemberDto>(new CommandDefinition(
            "SELECT Id, DisplayName, Email, Username, Role, IsActive FROM ITTeamMembers WHERE Id = @Id",
            new { Id = id }, cancellationToken: ct));
    }

    public async Task AssignAsync(int ticketId, ITTeamMemberDto member, string actor, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var current = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Status FROM AssetRequests WHERE Id = @Id", new { Id = ticketId }, transaction, cancellationToken: ct));
        if (current is null) throw new KeyNotFoundException("Ticket not found.");
        if (current is "Resolved" or "Closed" or "Cancelled") throw new InvalidOperationException("Completed tickets cannot be assigned.");

        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests
            SET AssignedToId = @AssignedToId,
                AssignedToName = @AssignedToName,
                AssignedAtUtc = COALESCE(AssignedAtUtc, @Now),
                Status = 'Assigned',
                LastUpdatedAtUtc = @Now
            WHERE Id = @Id;
            """, new { Id = ticketId, AssignedToId = member.Id, AssignedToName = member.DisplayName, Now = now }, transaction, cancellationToken: ct));

        await AddHistoryAsync(connection, transaction, ticketId, current, "Assigned", actor, member.DisplayName,
            $"Ticket assigned to {member.DisplayName}.", true, now, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task StartAsync(int ticketId, ITTeamMemberDto member, string actor, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var row = await connection.QuerySingleOrDefaultAsync<TicketOwnerRow>(new CommandDefinition(
            "SELECT Status, AssignedToId FROM AssetRequests WHERE Id = @Id", new { Id = ticketId }, transaction, cancellationToken: ct));
        if (row is null) throw new KeyNotFoundException("Ticket not found.");
        if (row.AssignedToId != member.Id) throw new InvalidOperationException("Only the assigned technician can start this ticket.");
        if (row.Status is "Resolved" or "Closed" or "Cancelled") throw new InvalidOperationException("Completed tickets cannot be started.");

        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests
            SET Status = 'In Progress', StartedAtUtc = COALESCE(StartedAtUtc, @Now), LastUpdatedAtUtc = @Now
            WHERE Id = @Id;
            """, new { Id = ticketId, Now = now }, transaction, cancellationToken: ct));

        await AddHistoryAsync(connection, transaction, ticketId, row.Status, "In Progress", actor, member.DisplayName,
            $"{member.DisplayName} started investigating the request.", true, now, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task UpdateStatusAsync(int ticketId, UpdateTicketStatusDto dto, string actor, string? actorName, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var current = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT Status FROM AssetRequests WHERE Id = @Id", new { Id = ticketId }, transaction, cancellationToken: ct));
        if (current is null) throw new KeyNotFoundException("Ticket not found.");

        var now = DateTime.UtcNow;
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE AssetRequests
            SET Status = @Status,
                ResolvedAtUtc = CASE WHEN @Status = 'Resolved' THEN COALESCE(ResolvedAtUtc, @Now) ELSE ResolvedAtUtc END,
                LastUpdatedAtUtc = @Now
            WHERE Id = @Id;
            """, new { Id = ticketId, Status = dto.Status, Now = now }, transaction, cancellationToken: ct));

        await AddHistoryAsync(connection, transaction, ticketId, current, dto.Status, actor, actorName,
            dto.Comment, dto.PublicVisible, now, ct);
        await transaction.CommitAsync(ct);
    }

    private static Task<int> AddHistoryAsync(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction,
        int ticketId, string? fromStatus, string toStatus, string changedBy, string? changedByName,
        string? comment, bool publicVisible, DateTime changedAtUtc, CancellationToken ct)
        => connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO TicketStatusHistory
                (TicketId, FromStatus, ToStatus, ChangedBy, ChangedByName, Comment, PublicVisible, ChangedAtUtc)
            VALUES
                (@TicketId, @FromStatus, @ToStatus, @ChangedBy, @ChangedByName, @Comment, @PublicVisible, @ChangedAtUtc);
            """, new { TicketId = ticketId, FromStatus = fromStatus, ToStatus = toStatus, ChangedBy = changedBy,
                ChangedByName = changedByName, Comment = comment, PublicVisible = publicVisible, ChangedAtUtc = changedAtUtc },
            transaction, cancellationToken: ct));
}
