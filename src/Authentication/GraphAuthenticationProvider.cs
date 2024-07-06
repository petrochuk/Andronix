using Andronix.Interfaces;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Runtime.InteropServices;

namespace Andronix.Authentication;

internal class GraphAuthenticationProvider : IAuthenticationProvider
{
    private static string[] GraphScope = ["https://graph.microsoft.com/User.Read Mail.ReadWrite Tasks.ReadWrite Chat.ReadWrite.All ChatMessage.Send"];
	IPublicClientApplication? _publicClientApplicationDefault;
    IPublicClientApplication? _publicClientApplicationForChats;
    private readonly IOptions<Core.Options.Graph> _graphOptions;
    IAccount? _account;
    IApplication _appHost;

    public GraphAuthenticationProvider(IOptions<Core.Options.Graph> graphOptions, IApplication appHost)
	{
        _graphOptions = graphOptions ?? throw new ArgumentNullException(nameof(graphOptions));
        _appHost = appHost ?? throw new ArgumentNullException(nameof(appHost));
	}

	async Task IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation request,
		Dictionary<string, object>? additionalAuthenticationContext,
		CancellationToken cancellationToken)
	{
        IPublicClientApplication publicClientApplication;
        var brokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows) 
        { 
            Title = "Andronix",
            ListOperatingSystemAccounts = true
        };

        if (request.URI.AbsolutePath.StartsWith("/beta/me/chats", StringComparison.OrdinalIgnoreCase))
        {
            if (_publicClientApplicationForChats == null)
            {
                var appBuilder = PublicClientApplicationBuilder.Create(_graphOptions.Value.ChatsClientId)
                    .WithLogging((level, message, containsPii) => {
                        Debug.WriteLine($"MSAL: {level} {message} ");
                    }, LogLevel.Verbose, enablePiiLogging: true, enableDefaultPlatformLogging: true)
                    .WithParentActivityOrWindow(_appHost.GetMainWindowHandle)
                    .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    appBuilder.WithBroker(brokerOptions);
                }

                _publicClientApplicationForChats = appBuilder.Build();
            }
            publicClientApplication = _publicClientApplicationForChats;
        }
        else
        {
            if (_publicClientApplicationDefault == null)
            {
                var appBuilder = PublicClientApplicationBuilder.Create(_graphOptions.Value.ClientId)
                    .WithLogging((level, message, containsPii) =>
                    {
                        Debug.WriteLine($"MSAL: {level} {message} ");
                    }, LogLevel.Verbose, enablePiiLogging: true, enableDefaultPlatformLogging: true)
                    .WithParentActivityOrWindow(_appHost.GetMainWindowHandle)
                    .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    appBuilder.WithBroker(brokerOptions);
                }

                _publicClientApplicationDefault = appBuilder.Build();
            }
            publicClientApplication = _publicClientApplicationDefault;
        }

        if (_account == null)
        {
            var accounts = await publicClientApplication.GetAccountsAsync();
            if (accounts.Count() == 1) 
                _account = accounts.First();
            else
                _account = PublicClientApplication.OperatingSystemAccount;
        }

        AuthenticationResult result;
        try 
        {
            result = await publicClientApplication.AcquireTokenSilent(
                GraphScope, _account).ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (MsalUiRequiredException ex) {
            try {
                // If the token has expired, prompt the user with a login prompt
                result = await publicClientApplication.AcquireTokenInteractive(GraphScope)
                        .WithAccount(PublicClientApplication.OperatingSystemAccount)
                        .WithClaims(ex.Claims)
                        .ExecuteAsync();

            }
            catch (Exception msalex) {
                Debug.WriteLine($"Error Acquiring Token:{Environment.NewLine}{msalex}");
                throw;
            }
        }

        request.Headers.Add("Authorization", [$"Bearer {result.AccessToken}"]);
	}
}
