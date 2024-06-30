using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Runtime.InteropServices;

namespace Andronix.Authentication;

internal class GraphAuthenticationProvider : IAuthenticationProvider
{
	IPublicClientApplication? _publicClientApplicationDefault;
    IPublicClientApplication? _publicClientApplicationForChats;
    private readonly IOptions<Core.Options.Graph> _graphOptions;

    public GraphAuthenticationProvider(IOptions<Core.Options.Graph> graphOptions)
	{
        _graphOptions = graphOptions ?? throw new ArgumentNullException(nameof(graphOptions));
	}

	async Task IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation request,
		Dictionary<string, object>? additionalAuthenticationContext,
		CancellationToken cancellationToken)
	{
        IPublicClientApplication publicClientApplication;

        if (request.URI.AbsolutePath.StartsWith("/beta/me/chats", StringComparison.OrdinalIgnoreCase))
        {
            if (_publicClientApplicationForChats == null)
            {
                var appBuilder = PublicClientApplicationBuilder.Create(_graphOptions.Value.ChatsClientId);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
                }

                _publicClientApplicationForChats = appBuilder.Build();
            }
            publicClientApplication = _publicClientApplicationForChats;
        }
        else
        {
            if (_publicClientApplicationDefault == null)
            {
                var appBuilder = PublicClientApplicationBuilder.Create(_graphOptions.Value.ClientId);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
                }

                _publicClientApplicationDefault = appBuilder.Build();
            }
            publicClientApplication = _publicClientApplicationDefault;
        }

        var result = await publicClientApplication.AcquireTokenSilent(
            ["https://graph.microsoft.com//.default"],
            PublicClientApplication.OperatingSystemAccount).ExecuteAsync(cancellationToken).ConfigureAwait(false);

		request.Headers.Add("Authorization", [$"Bearer {result.AccessToken}"]);
	}
}
