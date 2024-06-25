namespace Andronix.AssistantAI;

public class OpenAIResponse<T>
{
    public OpenAIResponse()
    {
        data = new List<T>();
    }

    public List<T> data { get; set; }
    public string first_id { get; set; }
    public string last_id { get; set; }
    public bool has_more { get; set; }
}
