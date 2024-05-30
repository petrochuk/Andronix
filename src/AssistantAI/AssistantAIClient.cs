using Andronix.Authentication;
using Andronix.Core;
using Azure.AI.OpenAI;

namespace Andronix.AssistantAI;

public class AssistantAIClient: OpenAIClient
{
    public AssistantAIClient(IOptions<CognitiveOptions> cognitiveOptions, AndronixTokenCredential andronixTokenCredential) 
        : base(cognitiveOptions.Value.EndPoint, andronixTokenCredential)
    {
    }
}
