using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.StaffAccess;

public sealed class StaffAccessService(IDbConnectionFactory connectionFactory, IConfiguration configuration)
{
    private const int Iterations = 120_000;

    public async Task<bool> BootstrapRequiredAsync(CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM StaffPortalAccess", cancellationToken: ct)) == 0;
    }

    public async Task<StaffProfileDto> BootstrapAsync(BootstrapStaffDto dto, CancellationToken ct = default)
    {
        if (!await BootstrapRequiredAsync(ct)) throw new InvalidOperationException("RoyalDesk staff access is already configured.");
        var expected = configuration["ROYALDESK_BOOTSTRAP_KEY"];
        if (string.IsNullOrWhiteSpace(expected)) throw new InvalidOperationException("ROYALDESK_BOOTSTRAP_KEY is not configured on the server.");
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(dto.BootstrapKey ?? string.Empty)))
            throw new UnauthorizedAccessException("Invalid bootstrap key.");

        return await CreateAsync(new CreateStaffAccessDto
        {
            DisplayName = dto.DisplayName,
            Username = dto.Username,
            Password = dto.Password,
            Pages = RoyalDeskPages.All.ToList()
        }, "bootstrap", ct);
    }

    public async Task<IReadOnlyList<StaffProfileDto>> GetProfilesAsync(bool activeOnly, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var rows = (await connection.QueryAsync<StaffProfileDto>(new CommandDefinition("""
            SELECT Id, DisplayName, Username, IsActive, LastAccessAtUtc
            FROM StaffPortalAccess
            WHERE (@ActiveOnly = 0 OR IsActive = 1)
            ORDER BY DisplayName;
            """, new { ActiveOnly = activeOnly ? 1 : 0 }, cancellationToken: ct))).AsList();
        foreach (var row in rows)
            row.Pages = (await connection.QueryAsync<string>(new CommandDefinition("SELECT PageKey FROM StaffPageAccess WHERE StaffAccessId=@Id ORDER BY PageKey", new { row.Id }, cancellationToken: ct))).ToList();
        return rows;
    }

    public async Task<StaffProfileDto> CreateAsync(CreateStaffAccessDto dto, string actor, CancellationToken ct = default)
    {
        Validate(dto.DisplayName, dto.Password, dto.Pages);
        var pages = NormalisePages(dto.Pages);
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = HashPassword(dto.Password, salt);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        var id = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            INSERT INTO StaffPortalAccess
                (DisplayName, Username, PasswordHash, PasswordSalt, IsActive, SessionVersion, FailedAttempts, CreatedBy, CreatedAtUtc, PasswordChangedAtUtc)
            VALUES (@DisplayName, @Username, @PasswordHash, @PasswordSalt, 1, 1, 0, @CreatedBy, @Now, @Now);
            SELECT last_insert_rowid();
            """, new
        {
            DisplayName = dto.DisplayName.Trim(),
            Username = string.IsNullOrWhiteSpace(dto.Username) ? null : dto.Username.Trim(),
            PasswordHash = Convert.ToBase64String(hash),
            PasswordSalt = Convert.ToBase64String(salt),
            CreatedBy = actor,
            Now = DateTime.UtcNow
        }, tx, cancellationToken: ct));

        foreach (var page in pages)
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO StaffPageAccess (StaffAccessId, PageKey) VALUES (@Id,@Page)", new { Id = id, Page = page }, tx, cancellationToken: ct));

        await EnsureItTeamMemberAsync(connection, tx, dto.DisplayName, dto.Username, pages, true, ct);
        await tx.CommitAsync(ct);
        return (await GetProfilesAsync(false, ct)).Single(x => x.Id == id);
    }

    public async Task UpdateAsync(int id, UpdateStaffAccessDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName)) throw new ArgumentException("Display name is required.");
        var pages = NormalisePages(dto.Pages);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        var changed = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE StaffPortalAccess
            SET DisplayName=@DisplayName, Username=@Username, IsActive=@IsActive,
                SessionVersion=SessionVersion+1
            WHERE Id=@Id;
            """, new { Id = id, DisplayName = dto.DisplayName.Trim(), Username = string.IsNullOrWhiteSpace(dto.Username) ? null : dto.Username.Trim(), dto.IsActive }, tx, cancellationToken: ct));
        if (changed == 0) throw new KeyNotFoundException("Staff access profile not found.");

        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM StaffPageAccess WHERE StaffAccessId=@Id", new { Id = id }, tx, cancellationToken: ct));
        foreach (var page in pages)
            await connection.ExecuteAsync(new CommandDefinition("INSERT INTO StaffPageAccess (StaffAccessId, PageKey) VALUES (@Id,@Page)", new { Id = id, Page = page }, tx, cancellationToken: ct));

        await EnsureItTeamMemberAsync(connection, tx, dto.DisplayName, dto.Username, pages, dto.IsActive, ct);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM StaffAccessSessions WHERE StaffAccessId=@Id", new { Id = id }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task ResetPasswordAsync(int id, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8) throw new ArgumentException("Password must be at least 8 characters.");
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = HashPassword(password, salt);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        var changed = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE StaffPortalAccess SET PasswordHash=@Hash, PasswordSalt=@Salt,
                PasswordChangedAtUtc=@Now, SessionVersion=SessionVersion+1, FailedAttempts=0, LockedUntilUtc=NULL
            WHERE Id=@Id;
            """, new { Id = id, Hash = Convert.ToBase64String(hash), Salt = Convert.ToBase64String(salt), Now = DateTime.UtcNow }, tx, cancellationToken: ct));
        if (changed == 0) throw new KeyNotFoundException("Staff access profile not found.");
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM StaffAccessSessions WHERE StaffAccessId=@Id", new { Id = id }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task<(StaffSessionDto Session, string RawToken)> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new UnauthorizedAccessException("Incorrect username or password.");
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var id = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
            SELECT Id FROM StaffPortalAccess
            WHERE lower(coalesce(nullif(trim(Username), ''), trim(DisplayName))) = lower(@Username)
              AND IsActive = 1 LIMIT 1;
            """, new { Username = username.Trim() }, cancellationToken: ct));
        if (!id.HasValue) throw new UnauthorizedAccessException("Incorrect username or password.");
        return await UnlockAsync(new UnlockStaffDto { StaffAccessId = id.Value, Password = password }, ct);
    }

    public async Task<(StaffSessionDto Session, string RawToken)> UnlockAsync(UnlockStaffDto dto, CancellationToken ct = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<AccessRow>(new CommandDefinition("SELECT * FROM StaffPortalAccess WHERE Id=@Id", new { Id = dto.StaffAccessId }, cancellationToken: ct));
        if (row is null || !row.IsActive) throw new UnauthorizedAccessException("Access is not available for this staff profile.");
        var now = DateTime.UtcNow;
        if (row.LockedUntilUtc.HasValue && row.LockedUntilUtc > now) throw new UnauthorizedAccessException("Too many incorrect attempts. Try again later.");
        var ok = VerifyPassword(dto.Password, row.PasswordSalt, row.PasswordHash);
        if (!ok)
        {
            var attempts = row.FailedAttempts + 1;
            var locked = attempts >= 5 ? now.AddMinutes(10) : (DateTime?)null;
            await connection.ExecuteAsync(new CommandDefinition("UPDATE StaffPortalAccess SET FailedAttempts=@Attempts, LockedUntilUtc=@Locked WHERE Id=@Id", new { Id = row.Id, Attempts = attempts >= 5 ? 0 : attempts, Locked = locked }, cancellationToken: ct));
            throw new UnauthorizedAccessException("Incorrect password.");
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Sha256(rawToken);
        var expires = now.AddHours(8);
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE StaffPortalAccess SET FailedAttempts=0, LockedUntilUtc=NULL, LastAccessAtUtc=@Now WHERE Id=@Id;
            INSERT INTO StaffAccessSessions (StaffAccessId, TokenHash, SessionVersion, CreatedAtUtc, LastSeenAtUtc, ExpiresAtUtc)
            VALUES (@Id,@TokenHash,@SessionVersion,@Now,@Now,@Expires);
            """, new { Id = row.Id, TokenHash = tokenHash, row.SessionVersion, Now = now, Expires = expires }, cancellationToken: ct));
        var pages = (await connection.QueryAsync<string>(new CommandDefinition("SELECT PageKey FROM StaffPageAccess WHERE StaffAccessId=@Id ORDER BY PageKey", new { Id = row.Id }, cancellationToken: ct))).ToList();
        return (new StaffSessionDto { StaffAccessId = row.Id, DisplayName = row.DisplayName, Username = row.Username, Pages = pages, ExpiresAtUtc = expires }, rawToken);
    }

    public async Task<StaffSessionDto?> ResolveSessionAsync(string? rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return null;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(new CommandDefinition("""
            SELECT s.Id SessionId, s.StaffAccessId, s.SessionVersion, s.LastSeenAtUtc, s.ExpiresAtUtc,
                   a.DisplayName, a.Username, a.IsActive, a.SessionVersion CurrentSessionVersion
            FROM StaffAccessSessions s JOIN StaffPortalAccess a ON a.Id=s.StaffAccessId
            WHERE s.TokenHash=@Hash LIMIT 1;
            """, new { Hash = Sha256(rawToken) }, cancellationToken: ct));
        if (row is null || !row.IsActive || row.SessionVersion != row.CurrentSessionVersion || row.ExpiresAtUtc <= DateTime.UtcNow || row.LastSeenAtUtc <= DateTime.UtcNow.AddMinutes(-60)) return null;
        await connection.ExecuteAsync(new CommandDefinition("UPDATE StaffAccessSessions SET LastSeenAtUtc=@Now WHERE Id=@Id", new { Id = row.SessionId, Now = DateTime.UtcNow }, cancellationToken: ct));
        var pages = (await connection.QueryAsync<string>(new CommandDefinition("SELECT PageKey FROM StaffPageAccess WHERE StaffAccessId=@Id ORDER BY PageKey", new { Id = row.StaffAccessId }, cancellationToken: ct))).ToList();
        return new StaffSessionDto { StaffAccessId = row.StaffAccessId, DisplayName = row.DisplayName, Username = row.Username, Pages = pages, ExpiresAtUtc = row.ExpiresAtUtc };
    }

    public async Task LockAsync(string? rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return;
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM StaffAccessSessions WHERE TokenHash=@Hash", new { Hash = Sha256(rawToken) }, cancellationToken: ct));
    }

    private static async Task EnsureItTeamMemberAsync(DbConnection connection, DbTransaction tx, string displayName, string? username, IReadOnlyCollection<string> pages, bool accessActive, CancellationToken ct)
    {
        var effectiveUsername = string.IsNullOrWhiteSpace(username) ? displayName.Trim() : username.Trim();
        var existingId = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
            SELECT Id FROM ITTeamMembers
            WHERE lower(Username)=lower(@Username) OR lower(DisplayName)=lower(@DisplayName)
            LIMIT 1;
            """, new { Username = effectiveUsername, DisplayName = displayName.Trim() }, tx, cancellationToken: ct));

        if (!accessActive)
        {
            if (existingId.HasValue)
                await connection.ExecuteAsync(new CommandDefinition("UPDATE ITTeamMembers SET IsActive=0 WHERE Id=@Id", new { Id = existingId.Value }, tx, cancellationToken: ct));
            return;
        }

        if (!pages.Contains("tickets", StringComparer.OrdinalIgnoreCase)) return;

        if (existingId.HasValue)
        {
            await connection.ExecuteAsync(new CommandDefinition("UPDATE ITTeamMembers SET DisplayName=@DisplayName, IsActive=1 WHERE Id=@Id", new { Id = existingId.Value, DisplayName = displayName.Trim() }, tx, cancellationToken: ct));
            return;
        }

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO ITTeamMembers (DisplayName, Email, Username, Role, IsActive, CreatedAtUtc)
            VALUES (@DisplayName, NULL, @Username, 'IT Technician', 1, @Now);
            """, new { DisplayName = displayName.Trim(), Username = effectiveUsername, Now = DateTime.UtcNow }, tx, cancellationToken: ct));
    }

    private static void Validate(string displayName, string password, IEnumerable<string> pages)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8) throw new ArgumentException("Password must be at least 8 characters.");
        _ = NormalisePages(pages);
    }

    private static List<string> NormalisePages(IEnumerable<string>? pages)
    {
        var result = (pages ?? []).Distinct(StringComparer.OrdinalIgnoreCase).Select(x => x.Trim().ToLowerInvariant()).ToList();
        if (result.Any(x => !RoyalDeskPages.All.Contains(x))) throw new ArgumentException("One or more page permissions are invalid.");
        return result;
    }

    private static byte[] HashPassword(string password, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
    private static bool VerifyPassword(string password, string salt64, string hash64) => CryptographicOperations.FixedTimeEquals(HashPassword(password, Convert.FromBase64String(salt64)), Convert.FromBase64String(hash64));
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class AccessRow
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SessionVersion { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
    }

    private sealed class SessionRow
    {
        public int SessionId { get; set; }
        public int StaffAccessId { get; set; }
        public int SessionVersion { get; set; }
        public int CurrentSessionVersion { get; set; }
        public DateTime LastSeenAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public bool IsActive { get; set; }
    }
}
