using Andronix.AssistantAI;
using Andronix.Authentication;
using Andronix.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;

namespace Andronix.UI;

internal class Program
{
    public static IHost Host { get; private set; }

    public static void Main(string[] args)
    {
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var configuration = LoadConfiguration();

                services.AddSingleton<App>();
                services.AddSingleton<IApplication>(p => p.GetRequiredService<App>());
                services.AddSingleton<IActionApprover>(x => x.GetRequiredService<App>());
                services.AddHostedService<WinUIHostedService>();
                services.AddHostedService<QueuedAssistantService>();

                // Add options
                services.AddOptions<Core.Options.Cognitive>().Bind(configuration.GetSection(nameof(Core.Options.Cognitive)));
                services.AddOptions<Core.Options.Graph>().Bind(configuration.GetSection(nameof(Core.Options.Graph)));
                services.AddOptions<Core.Options.Assistant>().Bind(configuration.GetSection(nameof(Core.Options.Assistant)));
                services.AddOptions<TeamsAssistant>().Bind(configuration.GetSection("TeamsAssistant"));
                services.AddOptions<Core.Options.AzDevOps>().Bind(configuration.GetSection(nameof(Core.Options.AzDevOps)));
                services.AddOptions<Core.Options.Git>().Bind(configuration.GetSection(nameof(Core.Options.Git)));

                services.AddSingleton<MainWindow>();
                services.AddSingleton<IDialogPresenter>(provider => provider.GetService<MainWindow>());
                services.AddSingleton<AssistantAI.KnowledgeCollectors.Git>();
                services.AddSingleton<KnowledgeCollectorBase>();
                services.AddSingleton<KnowledgeRepository>();

                services.AddAuthentication(configuration);
                services.AddAssistantAI(configuration);
            }).Build();

        Host.Run();
    }


    private static IConfiguration LoadConfiguration()
    {
        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        if (string.IsNullOrWhiteSpace(oneDrive))
            throw new FileNotFoundException("OneDrive environment variable not found");

        var userAppSettings = Path.Combine(oneDrive, "Assistant", "appSettings.json");
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
            .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(userAppSettings, optional: true, reloadOnChange: true);

#if DEBUG
        builder.AddJsonFile("appSettings.Debug.json", optional: true, reloadOnChange: true);
#endif

        return builder.Build();
    }

}
