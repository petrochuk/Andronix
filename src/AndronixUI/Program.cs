﻿using Andronix.AssistantAI;
using Andronix.Authentication;
using Andronix.Core;
using Andronix.Core.Options;
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

                services.AddSingleton<IApplication, App>();
                services.AddHostedService<WinUIHostedService>();
                services.AddHostedService<QueuedAssistantService>();

                // Add options
                services.AddOptions<CognitiveOptions>().Bind(configuration.GetSection(nameof(CognitiveOptions)));
                services.AddOptions<GraphOptions>().Bind(configuration.GetSection(nameof(GraphOptions)));
                services.AddOptions<AssistantOptions>().Bind(configuration.GetSection(nameof(AssistantOptions)));
                services.AddOptions<TeamsAssistantOptions>().Bind(configuration.GetSection("TeamsAssistant"));
                services.AddOptions<AzDevOps>().Bind(configuration.GetSection(nameof(AzDevOps)));

                services.AddSingleton<MainWindow>();
                services.AddSingleton<IDialogPresenter>(provider => provider.GetService<MainWindow>());
                services.AddSingleton<TeamsKnowledgeCollector>();
                services.AddSingleton<KnowledgeRepository>();

                services.AddAuthentication(configuration);
                services.AddAssistantAI(configuration);
            }).Build();

        Host.Run();
    }


    private static IConfiguration LoadConfiguration()
    {
        var userAppSettings = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Andronix", "appSettings.json");
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
