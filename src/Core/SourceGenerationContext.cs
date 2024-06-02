using System.Text.Json.Serialization;

namespace Andronix.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
