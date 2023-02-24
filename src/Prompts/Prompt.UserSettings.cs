using PackagesTransfer.Models;
using Microsoft.Extensions.Logging;
using PPlus;
using System.Text.Json;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public Usersettings UserSettings(CancellationToken cancellationToken)
        {
            if (File.Exists(AppConstants.FileNameUserSettings))
            {
                _logger?.LogInformation($"Found file {AppConstants.FileNameUserSettings}");
                var aux = PromptPlus
                    .SliderSwitch("Would you like to use the last saved data?")
                    .OnValue("Yes")
                    .OffValue("No")
                    .Default(true)
                    .Run(cancellationToken);
                if (aux.IsAborted)
                {
                    ExitTanks(1);
                }
                if (aux.Value)
                {
                    return JsonSerializer.Deserialize<Usersettings>(File.ReadAllText(AppConstants.FileNameUserSettings))!;
                }
            }
            _logger?.LogInformation($"Not Found file {AppConstants.FileNameUserSettings}");
            var defaultsettings = new Defaultsettings();
            return new Usersettings
            {
                feedsource = defaultsettings.sourceuri,
                feedtarget = defaultsettings.targeturi,
                filterProtocoltype = defaultsettings.protocols,
                tmpfolder = defaultsettings.tmpfolder,
                prefixurifeedsource = defaultsettings.prefixurifeedsource,
                prefixurifeedtarget = defaultsettings.prefixurifeedtarget,
                prefixuripkgsource = defaultsettings.prefixuripkgsource,
                prefixuripkgtarget = defaultsettings.prefixuripkgtarget,
                sourceuri = defaultsettings.sourceuri,
                targeturi = defaultsettings.targeturi,
                typetransfer = AppConstants.BetweenAzuredevops
            };
        }
    }
}
