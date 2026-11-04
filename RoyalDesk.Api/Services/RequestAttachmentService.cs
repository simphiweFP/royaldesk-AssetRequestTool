using Dapper;
using Microsoft.AspNetCore.Http;
using RoyalDesk.Api.Data;

namespace RoyalDesk.Api.Services;

public sealed class RequestAttachmentService(IDbConnectionFactory connections)
{
    private const int MaxFiles = 5;
    private const int MaxFileBytes = 8 * 1024 * 1024;
    private const int MaxTotalBytes = 25 * 1024 * 1024;

    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".xls"] = "application/vnd.ms-excel",
        [".doc"] = "application/msword",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };
    private static readonly HashSet<string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xls", ".xlsx" };

    public async Task<List<ValidatedAttachment>> ValidateAsync(IReadOnlyList<IFormFile> files, bool stockCode, bool incident, CancellationToken ct)
    {
        if (stockCode && files.Count == 0) throw new ArgumentException("A supporting stock creation document is required.");
        if (files.Count > 0 && !stockCode && !incident) throw new ArgumentException("Attachments are only supported for problem reports and stock code creation.");
        if (files.Count > MaxFiles || files.Sum(f => f.Length) > MaxTotalBytes) throw new ArgumentException("Too many attachments or total upload size is over 25 MB.");
        var result = new List<ValidatedAttachment>();
        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > MaxFileBytes) throw new ArgumentException("Each attachment must be between 1 byte and 8 MB.");
            var extension = Path.GetExtension(file.FileName);
            if (!Allowed.TryGetValue(extension, out var type) || (stockCode && !DocumentTypes.Contains(extension)))
                throw new ArgumentException("Unsupported attachment type.");
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            var bytes = memory.ToArray();
            if (!MatchesSignature(extension, bytes)) throw new ArgumentException("Attachment content does not match its file type.");
            var safeName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 180) throw new ArgumentException("Invalid attachment filename.");
            result.Add(new ValidatedAttachment(safeName, type, bytes));
        }
        if (stockCode && !result.Any(x => DocumentTypes.Contains(Path.GetExtension(x.FileName))))
            throw new ArgumentException("A valid stock creation document is required.");
        return result;
    }

    private static bool MatchesSignature(string ext, byte[] data)
    {
        bool Prefix(params byte[] bytes) => data.Length >= bytes.Length && data.AsSpan(0, bytes.Length).SequenceEqual(bytes);
        return ext.ToLowerInvariant() switch
        {
            ".pdf" => Prefix(0x25, 0x50, 0x44, 0x46, 0x2D),
            ".png" => Prefix(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".jpg" or ".jpeg" => Prefix(0xFF, 0xD8, 0xFF),
            ".webp" => data.Length >= 12 && data.AsSpan(0,4).SequenceEqual("RIFF"u8) && data.AsSpan(8,4).SequenceEqual("WEBP"u8),
            ".docx" or ".xlsx" => Prefix(0x50,0x4B,0x03,0x04), // ZIP: detailed archive inspection should be added for deployment.
            ".doc" or ".xls" => Prefix(0xD0,0xCF,0x11,0xE0,0xA1,0xB1,0x1A,0xE1),
            _ => false
        };
    }

    public async Task SaveAsync(int ticketId, IReadOnlyList<ValidatedAttachment> files, CancellationToken ct)
    {
        if (files.Count == 0) return;
        await using var db = connections.CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        foreach (var file in files)
            await db.ExecuteAsync(new CommandDefinition("""
                INSERT INTO RequestAttachments (TicketId, FileName, ContentType, Content, SizeBytes, UploadedAtUtc)
                VALUES (@TicketId, @FileName, @ContentType, @Content, @SizeBytes, @UploadedAtUtc)
                """, new { TicketId = ticketId, file.FileName, file.ContentType, Content = file.Bytes,
                    SizeBytes = file.Bytes.Length, UploadedAtUtc = DateTime.UtcNow }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<AttachmentInfo>> ListAsync(int ticketId, CancellationToken ct)
    {
        await using var db = connections.CreateConnection();
        await db.OpenAsync(ct);
        var list = await db.QueryAsync<AttachmentInfo>(new CommandDefinition("""
            SELECT Id, FileName, ContentType, SizeBytes FROM RequestAttachments
            WHERE TicketId=@TicketId ORDER BY Id
            """, new { TicketId = ticketId }, cancellationToken: ct));
        return list.AsList();
    }

    public async Task<AttachmentContent?> ReadAsync(int ticketId, int id, CancellationToken ct)
    {
        await using var db = connections.CreateConnection();
        await db.OpenAsync(ct);
        return await db.QuerySingleOrDefaultAsync<AttachmentContent>(new CommandDefinition("""
            SELECT FileName, ContentType, Content FROM RequestAttachments
            WHERE TicketId=@TicketId AND Id=@Id
            """, new { TicketId = ticketId, Id = id }, cancellationToken: ct));
    }
}

public sealed record ValidatedAttachment(string FileName, string ContentType, byte[] Bytes);
public sealed class AttachmentInfo
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
}
public sealed class AttachmentContent
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public byte[] Content { get; set; } = [];
}
