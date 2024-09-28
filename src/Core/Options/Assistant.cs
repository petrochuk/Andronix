namespace Andronix.Core.Options;

/// <summary>
/// Options for the personal Assistant
/// </summary>
public class Assistant
{
    public required string Model { get; set; } = "gpt-4o";
    public required string Name { get; set; }
    public required string Instructions { get; init; }
    public required string AboutMe { get; init; }
    public required string UserSettings { get; set; }
}
