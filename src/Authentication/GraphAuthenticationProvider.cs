﻿using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Runtime.InteropServices;

namespace Andronix.Authentication;

internal class GraphAuthenticationProvider : IAuthenticationProvider
{
	IPublicClientApplication? _publicClientApplication;
    private readonly IOptions<Core.Options.Graph> _graphOptions;

    public GraphAuthenticationProvider(IOptions<Core.Options.Graph> graphOptions)
	{
        _graphOptions = graphOptions ?? throw new ArgumentNullException(nameof(graphOptions));
	}

	async Task IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation request,
		Dictionary<string, object>? additionalAuthenticationContext,
		CancellationToken cancellationToken)
	{
		if (_publicClientApplication == null)
		{
			var appBuilder = PublicClientApplicationBuilder.Create(_graphOptions.Value.ClientId);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				appBuilder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
			}

            _publicClientApplication = appBuilder.Build();
		}

		var result = await _publicClientApplication.AcquireTokenSilent(
            ["https://graph.microsoft.com//.default"],
            PublicClientApplication.OperatingSystemAccount).ExecuteAsync(cancellationToken).ConfigureAwait(false);

		request.Headers.Add("Authorization", [$"Bearer {result.AccessToken}"]);
	}
}
