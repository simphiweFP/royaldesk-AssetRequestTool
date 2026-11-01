using System.Net.Mail;
using RoyalDesk.Api.DTOs;

namespace RoyalDesk.Api.Validation;

public sealed class CreateAssetRequestValidator
{
    public static readonly string[] Branches =
        ["Durban Passenger", "Durban Commercial", "Phoenix", "Alberton", "Blackheath"];

    public static readonly string[] Departments =
        ["Administration", "Finance", "Human Resources", "IT", "Operations", "Sales", "Workshop"];

    public static readonly string[] ItemTypes =
        ["Laptop", "Monitor", "Keyboard", "Mouse", "Headset", "Printer", "Barcode Scanner", "POS Peripheral"];

    public static readonly string[] Reasons =
        ["New Starter", "Replacement", "Upgrade", "Damaged Equipment", "Additional Equipment"];

    public IDictionary<string, string[]> Validate(CreateAssetRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.RequesterName))
            errors[nameof(request.RequesterName)] = ["Employee name is required."];
        else if (request.RequesterName.Trim().Length > 150)
            errors[nameof(request.RequesterName)] = ["Employee name must be 150 characters or fewer."];

        if (!string.IsNullOrWhiteSpace(request.RequesterEmail) && !IsValidEmail(request.RequesterEmail))
            errors[nameof(request.RequesterEmail)] = ["Please enter a valid email address or leave it blank."];

        AddSelectionError(errors, nameof(request.Branch), request.Branch, Branches);
        AddSelectionError(errors, nameof(request.Department), request.Department, Departments);
        AddSelectionError(errors, nameof(request.ItemType), request.ItemType, ItemTypes);

        if (request.Quantity is < 1 or > 10)
            errors[nameof(request.Quantity)] = ["Quantity must be between 1 and 10."];

        var reason = request.Reason?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(reason))
        {
            errors[nameof(request.Reason)] =
                ["Please select a reason for the request."];
        }
        else if (!Reasons.Any(r => reason.Equals(r, StringComparison.OrdinalIgnoreCase) ||
                                   reason.StartsWith(r + " - ", StringComparison.OrdinalIgnoreCase)))
        {
            errors[nameof(request.Reason)] =
                ["Please select a valid reason for the request."];
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email.Trim());
            return address.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void AddSelectionError(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        IEnumerable<string> allowedValues)
    {
        if (!allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            errors[field] = [$"{field} is not supported."];
    }
}
