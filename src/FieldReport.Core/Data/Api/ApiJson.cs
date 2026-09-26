using System.Text.Json;
using System.Text.Json.Serialization;

namespace FieldReport.Core.Data.Api;

/// <summary>Ustawienia JSON zgodne z API: camelCase (JsonSerializerDefaults.Web) + enumy jako tekst („High”, „InProgress”).</summary>
public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
