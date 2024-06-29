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
        services.AddTransient(s => new GraphServiceClient(s.GetRequiredService<IAuthenticationProvider>()));
        services.AddTransient<Tools.Tasks>();
        services.AddTransient<Tools.Git>();
        services.AddTransient<Tools.AzDevOps>();
        services.AddTransient<TeamsAssistant>();
        services.AddSingleton<IBackgroundTaskQueue, AssistantTaskQueue>();

        return services;
    }
}
