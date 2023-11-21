using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebFlow.Helpers;

public static class JsonHelper
{
    public static bool TryDeserialize<TClass>(string? message, [NotNullWhen(true)] out TClass? result)
    {
        result = default;
        try
        {
            if (String.IsNullOrEmpty(message))
                return false;

            using var document = JsonDocument.Parse(message.AsMemory());
            if (document.RootElement.TryGetProperty("Error", out _))
                return false;

            result = document.RootElement.Deserialize<TClass>()!;
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };
}