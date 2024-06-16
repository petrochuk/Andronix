namespace Andronix.AssistantAI;

/// <summary>
/// Exception thrown when a function tool call fails.
/// The message will be displayed to the user through the assistant.
/// </summary>
internal class FunctionCallException : Exception
{
    public FunctionCallException(string message) : base(message)
    {
    }
}
