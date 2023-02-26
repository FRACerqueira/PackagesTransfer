using PPlus.Objects;
using PPlus;
using PackagesTransfer.Models;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public string TempFolder(string keyhistory, int minlength, string prompt, string promptdesc,string defaultvalue,Defaultsettings defaultsettings, CancellationToken cancellationToken)
        {
            ResultPromptPlus<string> input = PromptPlus.Readline(prompt, promptdesc)
                .EnabledHistory(true)
                .InitialValue(defaultvalue)
                .FinisWhenHistoryEnter(true)
                .MinimumPrefixLength(minlength)
                .TimeoutHistory(TimeSpan.FromDays(defaultsettings.timeouthistorydays))
                .MaxHistory((byte)defaultsettings.maxhistory)
                .FileNameHistory($"FeedTransfer.{keyhistory}")
                .SaveHistoryAtFinish(true)
                .AddValidator(PromptPlusValidators.Required())
                .Run(cancellationToken);
            if (input.IsAborted)
            {
                ExitTanks(1);
            }
            return input.Value;
        }
    }
}
