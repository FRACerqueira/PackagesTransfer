using PackagesTransfer.Models;
using PackagesTransfer.Protocols;
using Microsoft.Extensions.Logging;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public FeedDataInfo ReRunSelectAzureDevopsOrigin(FeedDataInfo dataInfo, HttpClient httpClient, string origin,Usersettings usersettings, Defaultsettings defaultsettings, CancellationToken stoppingToken)
        {
            FeedValue[] feeds;
            FeedValue seletedfeed;
            UpstreamSource[] denyupstream = Array.Empty<UpstreamSource>();
            ProcessReadPackges packageread;

            while (true)
            {
                feeds = dataInfo.Feeds;

                seletedfeed = SelectAzureFeed(feeds, dataInfo.Seleted!.name!, "Select a feed", "", defaultsettings, stoppingToken);

                var protocols = usersettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries);

                if (seletedfeed.upstreamSources.Any(x => protocols.Select(x => x.ToLowerInvariant()).Contains(x.protocol.ToLowerInvariant())))
                {
                    var unaccepted = Question(
                        $"Filter Unaccepted Upstream ({seletedfeed.upstreamSources.Count()})?",
                        "",
                        true,
                        stoppingToken);

                    _logger?.LogInformation($"Filter Unaccepted {origin} Upstream: {unaccepted}");
                    if (unaccepted)
                    {
                        denyupstream = UpstreamSource(seletedfeed.upstreamSources, "Choose Unaccepted Upstream Sources","", stoppingToken);
                    }

                }
                packageread = new ProcessReadPackges();
                var tryagain = false;
                foreach (var item in protocols)
                {
                    string readsource;
                    if (seletedfeed.project == null)
                    {
                        readsource = ProtocolsTransferConstant.UriPackageList
                            .Replace("{baseorg}", AzureDevopsPrefix(dataInfo.Uribase, dataInfo.Prefixurifeed, _logger), StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{feedname}", seletedfeed.name, StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{filtertype}", item, StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        readsource = ProtocolsTransferConstant.UriPackageScopedList
                            .Replace("{baseorg}", AzureDevopsPrefix(dataInfo.Uribase, dataInfo.Prefixurifeed, _logger), StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{projectname}", seletedfeed.project.name, StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{feedname}", seletedfeed.name, StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{filtertype}", item, StringComparison.InvariantCultureIgnoreCase);
                    }
                    var defsufix = string.Empty;
                    if (item == ProtocolsTransferConstant.NameNugetProtocol)
                    {
                        defsufix = defaultsettings.sufixnuget.Split(";", StringSplitOptions.RemoveEmptyEntries)[0];
                    }
                    else if (item == ProtocolsTransferConstant.NameNpmProtocol)
                    {
                        defsufix = defaultsettings.sufixnpm.Split(";", StringSplitOptions.RemoveEmptyEntries)[0];
                    }
                    else
                    {
                        _logger?.LogError($"Not Implemented Sufix protocol {item}");
                        throw new NotImplementedException($"Sufix protocol {item}");
                    }

                    var itempackageread = AzureReadPackges(item, defsufix, denyupstream,
                        httpClient,
                        defaultsettings.takequery,
                        readsource,
                        dataInfo.Passsword,
                        "Reading packages", "", stoppingToken);

                    if (itempackageread.ErrorMessage != null)
                    {
                        tryagain = Question("Try again", itempackageread.ErrorMessage!, true, stoppingToken);
                        _logger?.LogInformation($"Try again: {tryagain}");
                        if (!tryagain)
                        {
                            ExitTanks(0);
                        }
                        break;
                    }

                    var newpkg = packageread.Packages.ToList();
                    newpkg.AddRange(itempackageread.Packages);

                    packageread.DistinctQtd += itempackageread.DistinctQtd;
                    packageread.Packages = newpkg.ToArray();
                }
                if (tryagain)
                {
                    continue;
                }
                break;
            }

            _logger?.LogInformation($"AzureReadPackges {origin} uribase : {dataInfo.Uribase}");
            _logger?.LogInformation($"AzureReadPackges {origin} prefix feed: {dataInfo.Prefixurifeed}");
            _logger?.LogInformation($"AzureReadPackges {origin} prefix pkg : {dataInfo.Prefixuripkg}");
            _logger?.LogInformation($"AzureReadPackges {origin} Feed: {seletedfeed.name}");
            foreach (var item in denyupstream)
            {
                _logger?.LogInformation($"AzureReadPackges Unaccepted Upstream: {item.name}");
            }
            _logger?.LogInformation($"AzureReadPackges Distinct Package Count: {packageread!.DistinctQtd}");
            foreach (var item in packageread.Packages)
            {
                _logger?.LogInformation($"Packges: {item.Id} Ver:{item.Version}");
            }

            return new FeedDataInfo
            {
                Uribase = dataInfo.Uribase,
                Prefixurifeed = dataInfo.Prefixurifeed,
                Prefixuripkg = dataInfo.Prefixuripkg,
                Passsword = dataInfo.Passsword,
                Feeds = feeds,
                Seleted = seletedfeed,
                DenyUpstream = denyupstream,
                DistinctPackageCount = packageread!.DistinctQtd,
                Packages = packageread.Packages
            };

            static string? AzureDevopsPrefix(string value, string prefix, ILogger? logger)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    return value;
                }
                try
                {
                    Uri.TryCreate(value, UriKind.Absolute, out Uri? uriinput);
                    UriBuilder result = new(uriinput!.Scheme, $"{prefix}.{uriinput.Authority}", uriinput.Port, uriinput.PathAndQuery);
                    if (result.Uri.AbsoluteUri.EndsWith("/"))
                    {
                        return result.Uri.AbsoluteUri.Substring(0, result.Uri.AbsoluteUri.Length-1);
                    }
                    return result.Uri.AbsoluteUri;
                }
                catch (Exception ex)
                {
                    logger?.LogInformation($"AzureDevopsPrefix Error: {ex}");
                }
                return null;
            }
        }
    }
}
