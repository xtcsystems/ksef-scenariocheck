using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;

namespace XtcSystems.KsefScenarioCheck.Core.Serialization;

public static class CanonicalJson
{
    public static string ComputeObservationDigest(ReadOnlySpan<byte> bytes)
        => ComputeDigest(bytes, removeGeneratedAt: true, removePackDigest: false);

    public static string ComputePackDigest(ReadOnlySpan<byte> bytes)
        => ComputeDigest(bytes, removeGeneratedAt: false, removePackDigest: true);

    public static string ComputeRawDigest(ReadOnlySpan<byte> bytes)
        => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string ComputeDigest(ReadOnlySpan<byte> bytes, bool removeGeneratedAt, bool removePackDigest)
    {
        using JsonDocument document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = ContractValidator.MaximumJsonDepth
        });

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        }))
        {
            WriteCanonical(document.RootElement, writer, parentProperty: null, depth: 0, removeGeneratedAt, removePackDigest);
            writer.Flush();
        }

        return ComputeRawDigest(buffer.WrittenSpan);
    }

    private static void WriteCanonical(
        JsonElement element,
        Utf8JsonWriter writer,
        string? parentProperty,
        int depth,
        bool removeGeneratedAt,
        bool removePackDigest)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject().OrderBy(static item => item.Name, StringComparer.Ordinal))
                {
                    if (removeGeneratedAt && depth == 0 && property.NameEquals("generatedAt"))
                    {
                        continue;
                    }

                    if (removePackDigest && depth == 1 && string.Equals(parentProperty, "manifest", StringComparison.Ordinal) && property.NameEquals("packDigest"))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer, property.Name, depth + 1, removeGeneratedAt, removePackDigest);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    WriteCanonical(item, writer, parentProperty, depth + 1, removeGeneratedAt, removePackDigest);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
