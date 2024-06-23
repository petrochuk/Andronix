using Andronix.Core.Options;
using Andronix.Interfaces;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System.Runtime.InteropServices;

namespace Andronix.Authentication;

public class AzDevOpsAuthProvider : ITokenProvider
{
    internal static string[] DevOpsScopes = ["https://app.vssps.visualstudio.com/user_impersonation"]; //Constant value to target Azure DevOps. Do not change  

    IPublicClientApplication? _publicClientApplication;
    private readonly IOptions<AzDevOps> _options;

    public AzDevOpsAuthProvider(IOptions<AzDevOps> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<AuthenticationResult> AquireTokenSilent(CancellationToken cancellationToken = default)
    {
        if (_publicClientApplication == null)
        {
            var appBuilder = PublicClientApplicationBuilder.Create(_options.Value.ClientId);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
            }

            _publicClientApplication = appBuilder.Build();
        }

        var result = await _publicClientApplication.AcquireTokenSilent(
            DevOpsScopes,
            PublicClientApplication.OperatingSystemAccount).ExecuteAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }
}
