using Microsoft.Extensions.Configuration;

namespace Andronix.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddTransient<AndronixTokenCredential>();

        return services;
    }
}
