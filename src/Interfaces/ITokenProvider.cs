using Microsoft.Identity.Client;

namespace Andronix.Interfaces;

public interface ITokenProvider
{
    Task<AuthenticationResult> AquireTokenSilent(CancellationToken cancellationToken = default);
}
