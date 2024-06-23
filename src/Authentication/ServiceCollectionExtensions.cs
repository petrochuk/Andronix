using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Andronix.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.AddTransient<AndronixTokenCredential>();
        services.AddTransient<IAuthenticationProvider, GraphAuthenticationProvider>();
        services.AddTransient<AzDevOpsAuthProvider>();

        return services;
    }
}
