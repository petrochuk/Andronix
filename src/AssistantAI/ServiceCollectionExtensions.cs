using Andronix.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Beta;

namespace Andronix.AssistantAI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssistantAI(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddHostedService<QueuedAssistantService>();
        services.AddTransient<Assistant>();
        services.AddTransient<GraphServiceClient>();
        services.AddSingleton<IBackgroundTaskQueue, AssistantTaskQueue>();

        return services;
    }
}
