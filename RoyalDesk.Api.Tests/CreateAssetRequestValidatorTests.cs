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
            Reason = "Replacement"
        };

        Assert.Empty(new CreateAssetRequestValidator().Validate(request));
    }

    [Fact]
    public void Validate_WithoutReason_ReturnsSelectionMessage()
    {
        var request = new CreateAssetRequestDto
        {
            Branch = "Phoenix",
            Department = "IT",
            ItemType = "Laptop",
            Quantity = 1,
            Reason = string.Empty
        };

        var errors = new CreateAssetRequestValidator().Validate(request);

        Assert.Equal(
            "Please select a reason for the request.",
            Assert.Single(errors[nameof(request.Reason)]));
    }

    [Fact]
    public void Validate_WithUnsupportedReason_ReturnsSelectionMessage()
    {
        var request = new CreateAssetRequestDto
        {
            Branch = "Phoenix",
            Department = "IT",
            ItemType = "Laptop",
            Quantity = 1,
            Reason = "Something Else"
        };

        var errors = new CreateAssetRequestValidator().Validate(request);

        Assert.Equal(
            "Please select a valid reason for the request.",
            Assert.Single(errors[nameof(request.Reason)]));
    }
}
