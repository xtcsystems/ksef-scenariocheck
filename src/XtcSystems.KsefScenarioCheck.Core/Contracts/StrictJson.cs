using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace XtcSystems.KsefScenarioCheck.Core.Contracts;

internal static class StrictJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        AllowTrailingCommas = false,
        MaxDepth = ContractValidator.MaximumJsonDepth,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static T Deserialize<T>(byte[] bytes, int maximumBytes, string errorCode)
    {
        if (bytes.Length == 0 || bytes.Length > maximumBytes)
        {
            throw new ContractException(errorCode, "The supplied JSON document is empty or exceeds the accepted size limit.");
        }

        try
        {
            _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
            ValidateNoDuplicateProperties(bytes, errorCode);

            T? value = JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
            return value ?? throw new ContractException(errorCode, "The supplied JSON document is invalid.");
        }
        catch (ContractException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new ContractException(errorCode, "The supplied JSON document is invalid.");
        }
        catch (DecoderFallbackException)
        {
            throw new ContractException(errorCode, "The supplied JSON document is not valid UTF-8.");
        }
    }

    private static void ValidateNoDuplicateProperties(ReadOnlySpan<byte> bytes, string errorCode)
    {
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = ContractValidator.MaximumJsonDepth
        });

        var objectProperties = new Stack<HashSet<string>>();

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    objectProperties.Push(new HashSet<string>(StringComparer.Ordinal));
                    break;
                case JsonTokenType.PropertyName:
                    if (objectProperties.Count == 0)
                    {
                        throw new JsonException();
                    }

                    string name = reader.GetString() ?? throw new JsonException();
                    if (!objectProperties.Peek().Add(name))
                    {
                        throw new ContractException(errorCode, "Duplicate JSON properties are not accepted.");
                    }

                    break;
                case JsonTokenType.EndObject:
                    if (objectProperties.Count == 0)
                    {
                        throw new JsonException();
                    }

                    objectProperties.Pop();
                    break;
            }
        }

        if (objectProperties.Count != 0)
        {
            throw new JsonException();
        }
    }
}
