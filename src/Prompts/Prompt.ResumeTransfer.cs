using PPlus.Objects;
using PPlus;
using PackagesTransfer.Models;
using System.Text.Json;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public bool ResumeTransfer(ProcessTransfer processTransfer,Usersettings usersettings,Defaultsettings defaultsettings,  CancellationToken cancellationToken)
        {
            var lastsettings = JsonSerializer.Serialize(usersettings)!;

            PromptPlus.WriteLine($"Total packages published: {processTransfer.PackagesOk.Length}", ConsoleColor.Yellow);
            PromptPlus.WriteLine($"Total packages Erros: {processTransfer.PackagesErr.Length}", ConsoleColor.Yellow);
            var itemsresume = new List<string>();
            var lastselect = "Rerun again with other data";
            itemsresume.Add(lastselect);
            itemsresume.Add("Finish Feed Transfer");
            if (processTransfer.PackagesOk.Any())
            {
                itemsresume.Add($"Published packages Visualize");
            }
            if (processTransfer.PackagesErr.Any())
            {
                itemsresume.Add($"Failured packages Visualize");
            }
            var rerun = false;
            var endresume = false;
            while (!endresume)
            {
                ResultPromptPlus<string> opt = PromptPlus.Select<string>("Choose a option")
                    .AddItems(itemsresume)
                    .Default(lastselect)
                    .Run(cancellationToken);

                if (opt.IsAborted)
                {
                    ExitTanks(1);
                }
                lastselect = opt.Value;
                switch (lastselect[0..2].ToUpper())
                {
                    case "RE":
                        rerun = true;
                        endresume = true;
                        File.WriteAllText(AppConstants.FileNameUserSettings, lastsettings);
                        break;
                    case "FI":
                        endresume = true;
                        File.WriteAllText(AppConstants.FileNameUserSettings, lastsettings);
                        break;
                    case "PU":
                        PromptPlus.Select<PackageInfo>("Items published")
                            .AddItems(processTransfer.PackagesOk)
                            .PageSize(defaultsettings.pagelength)
                            .TextSelector(x => $"{x.Id}:{x.Version} ({x.Protocol})")
                            .DescriptionSelector(x => x.ResumeInfo ?? " ")
                            .Config(x => x.EnabledAbortKey(true))
                            .Run(cancellationToken);
                        break;
                    case "FA":
                        PromptPlus.Select<PackageInfo>("Items with failures")
                            .AddItems(processTransfer.PackagesErr)
                            .PageSize(defaultsettings.pagelength)
                            .TextSelector(x => $"{x.Id}:{x.Version} ({x.Protocol})")
                            .DescriptionSelector(x => x.ResumeInfo ?? " ")
                            .Config(x => x.EnabledAbortKey(true))
                            .Run(cancellationToken);
                        break;
                }
            }
            return rerun;
        }
    }
}
