using PackagesTransfer.Prompts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace PackagesTransfer;
public class Program
{
    static async Task Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
        IHost host = Host.CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .UseEnvironment("CLI")
                .ConfigureLogging((hostContext, services) => 
                {
                    services.ClearProviders();
                    services.AddFile($"Logs/FeedTransferPlus.log", 
                        retainedFileCountLimit: 3,
                        outputTemplate: "{Timestamp:o} [{Level:u3}] {Message} {NewLine}{Exception}");                
                }) 
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<PromptTransfer>();
                    services.AddHttpClient(AppConstants.HttpClientNameAzure);
                    services.AddHostedService<MainProgram>();
                }).Build();
        await host.RunAsync();
    }
}