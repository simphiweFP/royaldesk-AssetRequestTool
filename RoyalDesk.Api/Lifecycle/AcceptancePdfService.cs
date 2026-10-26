using System.Text;

namespace RoyalDesk.Api.Lifecycle;

public static class AcceptancePdfService
{
    public static byte[] Create(AssignmentDto assignment)
    {
        var lines = new[]
        {
            "ROYAL TYRES - I.T. EMPLOYEE ASSET ACCEPTANCE",
            $"Employee: {assignment.EmployeeName}",
            $"Asset: {assignment.AssetTag}",
            $"Assigned: {assignment.AssignedAtUtc:yyyy-MM-dd HH:mm} UTC",
            $"Condition: {assignment.ConditionAtAssignment}",
            $"Accepted: {(assignment.EmployeeAcceptedAtUtc.HasValue ? assignment.EmployeeAcceptedAtUtc.Value.ToString("yyyy-MM-dd HH:mm 'UTC'") : "Not yet accepted")}",
            $"Signature name: {assignment.EmployeeSignatureName ?? "-"}",
            "",
            "Acknowledgment:",
            "The equipment remains company property and is for work purposes.",
            "The employee is responsible for care, safekeeping and prompt reporting of loss or damage.",
            "All equipment must be returned on resignation, transfer, or company request."
        };

        var text = string.Join("\\n", lines.Select(Escape));
        var content = $"BT /F1 11 Tf 50 790 Td 14 TL ({text}) Tj ET".Replace(") Tj ET", ") Tj ET");
        // Render each line separately so PDF viewers do not depend on multiline literal handling.
        var sb = new StringBuilder("BT /F1 11 Tf 50 790 Td 14 TL ");
        foreach (var line in lines) sb.Append('(').Append(Escape(line)).Append(") Tj T* ");
        sb.Append("ET");
        var stream = sb.ToString();

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4");
        writer.Flush();
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
            writer.Flush();
        }
        var xref = ms.Position;
        writer.WriteLine($"xref\n0 {objects.Count + 1}\n0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++) writer.WriteLine($"{offsets[i]:D10} 00000 n ");
        writer.WriteLine($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        writer.Flush();
        return ms.ToArray();
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
