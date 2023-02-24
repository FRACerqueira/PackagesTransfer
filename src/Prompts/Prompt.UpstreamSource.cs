using PPlus;
using PackagesTransfer.Protocols;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public UpstreamSource[] UpstreamSource(UpstreamSource[] upstreamSources, string prompt, string promptdesc, CancellationToken cancellationToken)
        {
            var auxfilter = PromptPlus
                .MultiSelect<UpstreamSource>(prompt,promptdesc)
                .AddItems(upstreamSources)
                .AddDefaults(upstreamSources)
                .TextSelector(x => x.name ?? string.Empty)
                .DescriptionSelector(x => $"{x.upstreamSourceType}({x.location})")
                .Run(cancellationToken);
            if (auxfilter.IsAborted)
            {
                ExitTanks(1);
            }
            return auxfilter.Value.ToArray();
        }
    }
}
