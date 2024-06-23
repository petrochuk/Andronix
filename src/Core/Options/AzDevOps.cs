namespace Andronix.Core.Options;

public class AzDevOps
{
    public required string OrganizationUrl { get; init; }
    public required string ClientId { get; init; }
    public required string Project { get; init; }
    public required string AreaPath { get; init; }
    public required string Team { get; init; }
}
