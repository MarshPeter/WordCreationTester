using System.Text.Json;
using System.Text.Json.Serialization;

namespace WordCreationTester.Infrastructure
{
    public static class Json
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() } // enums as "assurance", etc.
        };
    }
}
