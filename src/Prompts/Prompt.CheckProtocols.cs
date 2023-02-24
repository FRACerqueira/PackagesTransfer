using PackagesTransfer.Models;
using PackagesTransfer.Protocols.Npm;
using Microsoft.Extensions.Logging;
using PPlus;
using PPlus.Objects;
using System.Runtime.InteropServices;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {

        public StatusProtocols CheckProtocols(int timeoutcheck, CancellationToken cancellationToken)
        {
            var chkprotocol = PromptPlus.WaitProcess("Find installed protocols")
              .AddProcess(new SingleProcess(async (stoprocess) =>
              {
                  var statusProtocols = new StatusProtocols();
                  try
                  {
                      var vernuget = NuGet.Common.ClientVersionUtility.GetNuGetAssemblyVersion();
                      var index = vernuget.IndexOf("+");
                      if (index > 0)
                      {
                          vernuget = vernuget[0..index];
                      }
                      statusProtocols = new StatusProtocols
                      {
                          EnabledNuget = true,
                          NugetVersion = vernuget,
                          EnabledNpm = false,
                          NpmVersion = null,
                          NpmUserFileLocation = null
                      };

                      (NpmVersion? Version, string? ErrorInfo) findmpm = await NpmCommands.Version(timeoutcheck,_logger, stoprocess);
                      (string? path, string? ErrorInfo) findusercfgmpm = await NpmCommands.UserConfigPath(timeoutcheck,_logger,stoprocess);
                      if (findmpm!.ErrorInfo == null)
                      {
                          statusProtocols.EnabledNpm = true;
                          statusProtocols.NpmVersion = findmpm.Version!.npm;
                          statusProtocols.NpmUserFileLocation = findusercfgmpm.path;
                      }
                      _logger?.LogInformation(RuntimeInformation.OSDescription);
                      _logger?.LogInformation($"Nuget protocol({statusProtocols.NugetVersion})");
                      _logger?.LogInformation($"Npm protocol({statusProtocols.NpmVersion ?? string.Empty})");
                      _logger?.LogInformation($"Npm user file({statusProtocols.NpmUserFileLocation ?? string.Empty})");
                  }
                  catch (Exception ex)
                  {
                      _logger?.LogError($"Error on CheckProtocols: {ex}");
                      throw;
                  }
                  return await Task.FromResult<object>(statusProtocols);
              }, processTextResult: (x) => ""))
              .Run(cancellationToken);

            if (chkprotocol.IsAborted)
            {
                ExitTanks(1);
            }
            return (StatusProtocols)chkprotocol.Value.First().ValueProcess;
        }
    }
}
