using System.Text.Json.Serialization;

namespace Andronix.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
[JsonSerializable(typeof(List<Dictionary<string, string>>))]
[JsonSerializable(typeof(string))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
