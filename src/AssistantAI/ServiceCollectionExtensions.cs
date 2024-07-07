using Andronix.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Beta;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Andronix.AssistantAI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAssistantAI(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddHostedService<QueuedAssistantService>();
        services.AddTransient<Assistant>();
        services.AddTransient<GraphServiceClient>();
        services.AddTransient<Tools.Tasks>();
        services.AddTransient<Tools.Git>();
        services.AddTransient<Tools.FileSystem>();
        services.AddTransient<Tools.AzDevOps>();
        services.AddTransient<Tools.Teams>();
        services.AddTransient<Tools.Outlook>();
        services.AddTransient<Tools.Notes>();
        services.AddTransient<TeamsAssistant>();
        services.AddSingleton<IBackgroundTaskQueue, AssistantTaskQueue>();

        return services;
    }
}
