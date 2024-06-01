using Andronix.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Threading;

namespace Andronix.UI;

public class WinUIHostedService : IHostedService, IDisposable
{
    private readonly IHostApplicationLifetime HostApplicationLifetime;
    private readonly IServiceProvider ServiceProvider;

    public WinUIHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IServiceProvider serviceProvider)
    {
        HostApplicationLifetime = hostApplicationLifetime;
        ServiceProvider = serviceProvider;
    }

    public void Dispose()
    {
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var thread = new Thread(Main);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) => {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            // Create a new instance of the application
            ServiceProvider.GetRequiredService<IApplication>();
        });
        HostApplicationLifetime.StopApplication();
    }
}