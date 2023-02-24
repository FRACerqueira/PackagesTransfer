using PPlus.Objects;
using PPlus;
using PackagesTransfer.Protocols;
using PackagesTransfer.Models;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public FeedValue SelectAzureFeed(FeedValue[] values,string defaultvalue, string prompt, string promptdesc, Defaultsettings defaultsettings , CancellationToken cancellationToken)
        {
            ResultPromptPlus<FeedValue> seletedfeed = PromptPlus
             .Select<FeedValue>(prompt, promptdesc)
             .AddItems(values)
             .TextSelector(x => $"{x.name!}{ScopedFeed(x)}")
             .Default(
                new FeedValue { name = defaultvalue },
                (x1, x2) => x1.name!.ToLower() == x2.name!.ToLower())
             .PageSize(defaultsettings.pagelength)
             .Run(cancellationToken);

            if (seletedfeed.IsAborted)
            {
                ExitTanks(1);
            }
            return seletedfeed.Value;

            static string ScopedFeed(FeedValue feed)
            {
                if (feed.project == null)
                    return "";
                return $" (Scope {feed.project.name!})";
            }
        }
    }
}
