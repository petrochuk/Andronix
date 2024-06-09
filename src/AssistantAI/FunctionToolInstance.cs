using OpenAI.Assistants;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace Andronix.AssistantAI;

public class FunctionToolInstance
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required MethodInfo MethodInfo { get; init; }

    public required FunctionToolDefinition Definition { get; init; }
    public async Task<string> Invoke(object instance, string argumentsJson)
    {
        Task<string> result;
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            result = (Task<string>)MethodInfo.Invoke(instance, null)!;
            return await result.ConfigureAwait(false);
        }

        var arguments = JsonSerializer.Deserialize<Dictionary<string, string>>(argumentsJson);
        if (arguments == null)
        {
            result = (Task<string>)MethodInfo.Invoke(instance, null)!;
            return await result.ConfigureAwait(false);
        }

        var functionParameters = new List<object?>();
        foreach (var parameter in MethodInfo.GetParameters())
        {
            if (arguments.TryGetValue(parameter.Name!, out var parameterValueString))
            {
                var typeConverter = TypeDescriptor.GetConverter(parameter.ParameterType);
                var parameterValue = typeConverter.ConvertFromString(parameterValueString);
                functionParameters.Add(parameterValue);
            }
            else
                functionParameters.Add(null);
        }

        result = (Task<string>)MethodInfo.Invoke(instance, functionParameters.ToArray())!;
        return await result.ConfigureAwait(false);
    }
}
