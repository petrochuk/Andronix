﻿namespace Andronix.Core.Options;

public record Cognitive
{
    public required Uri EndPoint { get; init; }
    public required string TenantIdForCognitiveServices { get; init; }
    public required string ClientIdForCognitiveServices { get; init; }
    public List<string>? KnowledgeFiles { get; init; }
    public required string EmbeddingModel { get; init; }
}
