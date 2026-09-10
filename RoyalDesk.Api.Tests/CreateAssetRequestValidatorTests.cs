using RoyalDesk.Api.DTOs;
using RoyalDesk.Api.Validation;

namespace RoyalDesk.Api.Tests;

public sealed class CreateAssetRequestValidatorTests
{
    [Fact]
    public void Validate_WithSupportedRequest_ReturnsNoErrors()
    {
        var request = new CreateAssetRequestDto
        {
            Branch = "Durban Commercial",
            Department = "Workshop",
            ItemType = "Barcode Scanner",
            Quantity = 1,
            Reason = "The existing scanner is damaged."
        };

        Assert.Empty(new CreateAssetRequestValidator().Validate(request));
    }

    [Fact]
    public void Validate_WithHtml_ReturnsReasonError()
    {
        var request = new CreateAssetRequestDto
        {
            Branch = "Phoenix",
            Department = "IT",
            ItemType = "Laptop",
            Quantity = 1,
            Reason = "<script>alert('xss')</script>"
        };

        Assert.Contains(nameof(request.Reason),
            new CreateAssetRequestValidator().Validate(request).Keys);
    }
}
