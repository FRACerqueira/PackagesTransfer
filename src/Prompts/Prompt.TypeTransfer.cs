using PackagesTransfer.Models;
using Microsoft.Extensions.Logging;
using PPlus;
using PPlus.Objects;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public string TypeTransfer(bool rerun, Usersettings usersettings, CancellationToken cancellationToken)
        {
            if (rerun)
            {
                var msgrerun = $"Type Transfer : {usersettings.typetransfer}";
                _logger?.LogInformation(msgrerun);
                PromptPlus.WriteLine(msgrerun, ConsoleColor.Yellow);
                return usersettings.typetransfer;
            }
            ResultPromptPlus<string> opt = PromptPlus.Select<string>("Select Type Transfer")
                .AddItems(AppConstants.TypesTransfer)
                .Default(usersettings.typetransfer)
                .Run(cancellationToken);

            if (opt.IsAborted)
            {
                ExitTanks(1);
            }
            var msg = $"Type Transfer : {opt.Value}";
            _logger?.LogInformation(msg);
            PromptPlus.WriteLine(msg, ConsoleColor.Yellow);
            return opt.Value;
        }

    }
}
