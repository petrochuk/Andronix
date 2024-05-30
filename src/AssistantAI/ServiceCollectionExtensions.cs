using Microsoft.Extensions.Configuration;

namespace Andronix.AssistantAI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssistantAI(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddTransient<AssistantAIClient>();

        return services;
    }
}
