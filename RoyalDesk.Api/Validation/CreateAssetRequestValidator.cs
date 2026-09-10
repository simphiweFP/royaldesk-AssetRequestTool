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

    public IDictionary<string, string[]> Validate(CreateAssetRequestDto request)
    {
        var errors = new Dictionary<string, string[]>();

        AddSelectionError(errors, nameof(request.Branch), request.Branch, Branches);
        AddSelectionError(errors, nameof(request.Department), request.Department, Departments);
        AddSelectionError(errors, nameof(request.ItemType), request.ItemType, ItemTypes);

        if (request.Quantity is < 1 or > 10)
            errors[nameof(request.Quantity)] = ["Quantity must be between 1 and 10."];

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500)
            errors[nameof(request.Reason)] = ["Reason must be between 10 and 500 characters."];
        else if (reason.Contains('<') || reason.Contains('>'))
            errors[nameof(request.Reason)] = ["Reason cannot contain HTML markup."];

        return errors;
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
