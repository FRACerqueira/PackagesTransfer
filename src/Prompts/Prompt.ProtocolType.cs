using PackagesTransfer.Models;
using PackagesTransfer.Protocols;
using Microsoft.Extensions.Logging;
using PPlus;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public string ProtocolType(bool rerun, Usersettings usersettings, StatusProtocols statusProtocols , CancellationToken cancellationToken)
        {
            if (rerun)
            {
                PromptPlus.WriteLine($"Protocol type filter : {usersettings.filterProtocoltype}", ConsoleColor.Yellow);
                _logger?.LogInformation($"Protocol type filters : {usersettings.filterProtocoltype}");
                return usersettings.filterProtocoltype;
            }

            var defvalue = usersettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!statusProtocols.EnabledNpm)
            {
                var index = defvalue.IndexOf(ProtocolsTransferConstant.NameNpmProtocol);
                if (index != -1) 
                {
                    defvalue.RemoveAt(index);
                }
            }

            var aux = PromptPlus
                .MultiSelect<string>("Protocol type filters")
                .AddItems(ProtocolsTransferConstant.NamesProtocol)
                .DisableItem(!statusProtocols.EnabledNpm ? ProtocolsTransferConstant.NameNpmProtocol : "")
                .AddDefaults(defvalue)
                .Range(1, ProtocolsTransferConstant.NamesProtocol.Length)
                .Run(cancellationToken);
            if (aux.IsAborted)
            {
                ExitTanks(1);
            }
            var types = string.Join(';', aux.Value);
            PromptPlus.WriteLine($"Protocol type filter : {types}", ConsoleColor.Yellow);
            _logger?.LogInformation($"Protocol type filters : {types}");
            return types;
        }
    }
}
