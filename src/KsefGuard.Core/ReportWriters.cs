using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KsefGuard;

public static class ReportWriters
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task WriteAsync(ValidationReport report, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var jsonPath = Path.Combine(outputDirectory, "report.json");
        var htmlPath = Path.Combine(outputDirectory, "report.html");

        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(htmlPath, BuildHtml(report), new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
    }

    private static string BuildHtml(ValidationReport report)
    {
        static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        builder.AppendLine("<title>KSeF Guard report</title>");
        builder.AppendLine("<style>body{font-family:system-ui,sans-serif;max-width:1100px;margin:2rem auto;padding:0 1rem;color:#1f2937}table{border-collapse:collapse;width:100%}th,td{border:1px solid #d1d5db;padding:.55rem;text-align:left;vertical-align:top}.Pass{color:#166534}.Fail{color:#b91c1c}.NeedsReview{color:#92400e}code{background:#f3f4f6;padding:.1rem .25rem}small{color:#4b5563}</style></head><body>");
        builder.AppendLine("<h1>KSeF Guard validation report</h1>");
        builder.Append("<p><strong>Pack:</strong> <code>").Append(E(report.PackId)).Append("</code> ").Append(E(report.PackVersion)).AppendLine("</p>");
        builder.Append("<p><strong>Status:</strong> <span class=\"").Append(report.Status).Append("\">").Append(report.Status).AppendLine("</span></p>");
        builder.Append("<p><strong>Run:</strong> ").Append(E(report.RunTimestamp.ToString("O"))).Append(" · <strong>Tool:</strong> ").Append(E(report.ToolVersion)).AppendLine("</p>");
        builder.Append("<p><strong>Summary:</strong> passed ").Append(report.Summary.Passed).Append(", failed ").Append(report.Summary.Failed).Append(", needs review ").Append(report.Summary.NeedsReview).AppendLine("</p>");

        foreach (var scenario in report.Scenarios)
        {
            builder.Append("<h2>").Append(E(scenario.Id)).Append(" <small>(").Append(E(scenario.Type)).AppendLine(")</small></h2>");
            builder.AppendLine("<table><thead><tr><th>Status</th><th>Code</th><th>English</th><th>Polski</th><th>Artifact</th><th>Detail</th></tr></thead><tbody>");
            foreach (var finding in scenario.Findings)
            {
                builder.Append("<tr><td class=\"").Append(finding.Status).Append("\">").Append(finding.Status).Append("</td><td><code>")
                    .Append(E(finding.Code)).Append("</code></td><td>").Append(E(finding.MessageEn)).Append("</td><td>")
                    .Append(E(finding.MessagePl)).Append("</td><td>").Append(E(finding.ArtifactPath)).Append("</td><td>")
                    .Append(E(finding.Detail)).AppendLine("</td></tr>");
            }

            builder.AppendLine("</tbody></table>");
        }

        builder.AppendLine("<hr><p><small>Independent, unofficial developer-tool output. This report is not tax, accounting, legal or compliance advice and is not an official KSeF certification.</small></p>");
        builder.AppendLine("</body></html>");
        return builder.ToString();
    }
}
