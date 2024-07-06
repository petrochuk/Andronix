using Andronix.Interfaces;
using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace Andronix.Authentication;

/// <summary>
/// Bearer Token Credential
/// </summary>
public class AndronixTokenCredential : TokenCredential
{
    private static IPublicClientApplication? PublicClientApplicationForCognitiveServices;
    public static string[] CognitiveServicesScope = ["https://cognitiveservices.azure.com/.default"];
    IOptions<Core.Options.Cognitive> _cognitiveOptions;

    public AndronixTokenCredential(IOptions<Core.Options.Cognitive> cognitiveOptions, IApplication appHost)
    {
        _cognitiveOptions = cognitiveOptions;
        if (PublicClientApplicationForCognitiveServices == null)
        {
            var brokerOptions = new BrokerOptions(BrokerOptions.OperatingSystems.Windows) 
            { 
                Title = appHost.Title,
                ListOperatingSystemAccounts = true
            };

            PublicClientApplicationForCognitiveServices = PublicClientApplicationBuilder.Create(cognitiveOptions.Value.ClientIdForCognitiveServices)
                .WithLogging((level, message, containsPii) =>
                {
                    Debug.WriteLine($"MSAL: {level} {message} ");
                }, LogLevel.Verbose, enablePiiLogging: true, enableDefaultPlatformLogging: true)
                .WithParentActivityOrWindow(appHost.GetMainWindowHandle)
                .WithTenantId(cognitiveOptions.Value.TenantIdForCognitiveServices)
                .WithBroker(brokerOptions)
                .Build();
        }
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        if (PublicClientApplicationForCognitiveServices == null)
            throw new InvalidOperationException($"{nameof(PublicClientApplicationForCognitiveServices)} is not initialized.");

        AuthenticationResult result;

        try
        {
            var accounts = await PublicClientApplicationForCognitiveServices.GetAccountsAsync();
            if (accounts.Count() == 1) 
            {
                // Attempt to get a token from the cache (or refresh it silently if needed)
                result = await PublicClientApplicationForCognitiveServices.AcquireTokenSilent(
                    CognitiveServicesScope, accounts.First()).ExecuteAsync();
            }
            else
            {
                // Attempt to get a token from the cache (or refresh it silently if needed)
                result = await PublicClientApplicationForCognitiveServices.AcquireTokenSilent(
                    CognitiveServicesScope, PublicClientApplication.OperatingSystemAccount)
                    .ExecuteAsync();
            }
        }
        catch (MsalUiRequiredException ex)
        {
            try
            {
                // If the token has expired, prompt the user with a login prompt
                result = await PublicClientApplicationForCognitiveServices.AcquireTokenInteractive(CognitiveServicesScope)
                        .WithAccount(PublicClientApplication.OperatingSystemAccount)
                        .WithClaims(ex.Claims)
                        .ExecuteAsync();

            }
            catch (Exception msalex)
            {
                Debug.WriteLine($"Error Acquiring Token:{Environment.NewLine}{msalex}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error Acquiring Token Silently:{Environment.NewLine}{ex}");
            throw;
        }

        return new AccessToken(result.AccessToken, result.ExpiresOn);
    }
}
