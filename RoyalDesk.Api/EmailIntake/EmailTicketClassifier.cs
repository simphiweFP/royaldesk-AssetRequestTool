using RoyalDesk.Api.Validation;

namespace RoyalDesk.Api.EmailIntake;

/// <summary>
/// Deterministic suggestions for incoming Microsoft 365 messages.
/// Classification never implies permission to create or approve an equipment request.
/// Missing employee identity/branch/department are resolved in a separate triage step.
/// </summary>
public sealed record EmailCategorySuggestion(
    string RequestType,
    string Category,
    string Confidence,
    bool RequiresTriage,
    string Rule);

public static class EmailTicketClassifier
{
    private sealed record Rule(string RequestType, string Category, string[] Terms);

    private static readonly Rule[] Rules =
    [
        new("Service", "Stock Code Creation", ["stock code", "item code creation", "create a sku"]),
        new("Service", "Shared Folder", ["shared folder access", "create shared folder", "file share request"]),
        new("Service", "Software Installation", ["install software", "software installation", "install application"]),
        new("Service", "User Account", ["create user account", "new user account", "account creation"]),
        new("Service", "Access / Permissions", ["grant access", "request access", "permission request", "give access"]),
        new("Service", "Custom Software", ["build application", "new software feature", "custom software"]),
        new("Service", "IT Advice", ["it advice", "technology advice", "recommend software"]),
        new("Equipment", "Laptop", ["new laptop", "need a laptop", "request laptop", "laptop replacement"]),
        new("Equipment", "Monitor", ["new monitor", "need a monitor", "request monitor", "second screen"]),
        new("Equipment", "Keyboard", ["new keyboard", "need a keyboard", "keyboard replacement"]),
        new("Equipment", "Mouse", ["new mouse", "need a mouse", "mouse replacement"]),
        new("Equipment", "Headset", ["new headset", "request headset"]),
        new("Equipment", "Barcode Scanner", ["new barcode scanner", "request barcode scanner"]),
        new("Equipment", "POS Peripheral", ["request pos peripheral", "new pos peripheral"]),
        new("Equipment", "Printer", ["new printer", "request printer", "printer purchase"]),
        new("Incident", "SAP", ["sap error", "sap cannot", "sap login", "sap not working", "sap down", "cannot log into sap"]),
        new("Incident", "CisTech", ["cistech error", "cistech not working", "cistech down"]),
        new("Incident", "Email", ["outlook error", "cannot send email", "mailbox not syncing", "email not working"]),
        new("Incident", "Network", ["no internet", "wifi down", "wi-fi down", "network disconnect", "network down"]),
        new("Incident", "Printer", ["printer not working", "printer offline", "paper jam", "cannot print"]),
        new("Incident", "Telephone", ["phone not working", "telephone not working", "voip down"]),
        new("Incident", "Windows", ["windows error", "windows update failed", "blue screen"]),
        new("Incident", "Hardware", ["laptop not working", "broken laptop", "damaged screen", "keyboard not working", "mouse not working"]),
        new("Incident", "Software", ["software error", "application crashed", "app not working"])
    ];

    public static EmailCategorySuggestion Suggest(string? subject, string? plainBody)
    {
        var text = (subject + " " + plainBody).ToLowerInvariant();
        var matches = Rules.Where(rule => rule.Terms.Any(term => text.Contains(term, StringComparison.Ordinal))).ToList();

        // Never choose the first of conflicting categories/types silently.
        if (matches.Count == 1)
            return Validate(matches[0], "High", false, "Matched approved phrase");
        if (matches.Count > 1 && matches.Select(m => m.RequestType + "/" + m.Category).Distinct().Count() == 1)
            return Validate(matches[0], "High", false, "Multiple matching phrases for the same category");

        return new EmailCategorySuggestion("Incident", "Other", "Low", true,
            matches.Count == 0 ? "No reliable category match" : "Conflicting category matches");
    }

    private static EmailCategorySuggestion Validate(Rule rule, string confidence, bool triage, string reason)
    {
        string[] valid = rule.RequestType switch
        {
            "Incident" => CreateAssetRequestValidator.IncidentCategories,
            "Service" => CreateAssetRequestValidator.ServiceCategories,
            "Equipment" => CreateAssetRequestValidator.EquipmentTypes,
            _ => []
        };
        if (!valid.Contains(rule.Category, StringComparer.Ordinal))
            throw new InvalidOperationException("Incoming email classification refers to an unsupported RoyalDesk category.");
        return new EmailCategorySuggestion(rule.RequestType, rule.Category, confidence, triage, reason);
    }
}
