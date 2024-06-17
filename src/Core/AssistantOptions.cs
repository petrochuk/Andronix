namespace Andronix.Core;

/// <summary>
/// Options for the personal Assistant
/// </summary>
public class AssistantOptions
{
    public required string Name { get; set; }
    public required string Instructions { get; init; }
    public required string AboutMe { get; init; }
    public required string UserSettings { get; set; }
}
