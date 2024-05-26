using Azure;
using Azure.AI.OpenAI;
using System;

namespace Andronix.OpenAI;

public class AssistantAIClient: OpenAIClient
{
    public AssistantAIClient() 
        : base(
            new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")), 
            new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")))
    {
    }
}
