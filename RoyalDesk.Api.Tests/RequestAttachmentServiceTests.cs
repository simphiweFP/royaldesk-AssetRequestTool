using Microsoft.AspNetCore.Http;
using RoyalDesk.Api.Services;

namespace RoyalDesk.Api.Tests;

public sealed class RequestAttachmentServiceTests
{
    private readonly RequestAttachmentService _service = new(null!);

    [Fact]
    public async Task StockCode_RequiresSupportingDocument()
    {
        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ValidateAsync([], stockCode: true, incident: false, CancellationToken.None));
        Assert.Contains("required", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Incident_AllowsPhotoEvidence()
    {
        var image = Upload("evidence.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);
        var files = await _service.ValidateAsync([image], stockCode: false, incident: true, CancellationToken.None);
        Assert.Single(files);
        Assert.Equal("evidence.png", files[0].FileName);
    }

    [Fact]
    public async Task InvalidFileSignature_IsRejected()
    {
        var file = Upload("suspicious.pdf", [0x00, 0x01, 0x02]);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ValidateAsync([file], stockCode: true, incident: false, CancellationToken.None));
    }

    [Fact]
    public async Task Equipment_DoesNotAcceptAttachments()
    {
        var image = Upload("evidence.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.ValidateAsync([image], stockCode: false, incident: false, CancellationToken.None));
    }

    private static IFormFile Upload(string name, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, stream.Length, "files", name);
    }
}
