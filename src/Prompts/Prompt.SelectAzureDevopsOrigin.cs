using PPlus;
using PackagesTransfer.Models;
using PackagesTransfer.Protocols;
using Microsoft.Extensions.Logging;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public FeedDataInfo SelectAzureDevopsOrigin(HttpClient httpClient, string origin, Usersettings usersettings, Defaultsettings defaultsettings, CancellationToken stoppingToken)
        {
            string uribase;
            string prefixfeed;
            string prefixpkg;
            string password;
            string defvalue;
            FeedValue[] feeds;
            FeedValue seletedfeed;
            UpstreamSource[] denyupstream = Array.Empty<UpstreamSource>();
            ProcessReadPackges packageread;

            if (AppConstants.IsOriSource(origin))
            {
                uribase = usersettings.sourceuri;
                prefixfeed = usersettings.prefixurifeedsource;
                prefixpkg = usersettings.prefixuripkgsource;
                defvalue = usersettings.feedsource;
            }
            else if (AppConstants.IsOriTarget(origin))
            {
                uribase = usersettings.targeturi;
                prefixfeed = usersettings.prefixurifeedtarget;
                prefixpkg = usersettings.prefixuripkgtarget;
                defvalue = usersettings.feedtarget;
            }
            else
            {
                _logger?.LogError($"SelectAzureDevopsOrigin, Invalid param origin : {origin}");
                throw new ArgumentException($"Invalid value : {origin}", nameof(origin));
            }

            while (true)
            {
                uribase = SelectUriFeed(origin,
                "Uri",
                $"Uri {origin} Azure-Devops with organization",
                uribase,
                defaultsettings,
                stoppingToken);

                prefixfeed = SelectUriPrefix(origin,
                    "Prefix feed-sub-domain Uri",
                    $"Ex: {AzureDevopsPrefix(uribase, ProtocolsTransferConstant.ExamplePrefixfeeds,_logger)}",
                    prefixfeed,
                    defaultsettings,
                    stoppingToken);

                prefixpkg = SelectUriPrefix(origin,
                    "Prefix package-sub-domain Uri",
                    $"Ex: {AzureDevopsPrefix(uribase, ProtocolsTransferConstant.ExamplePrefixpkgs,_logger)}",
                    prefixpkg,
                    defaultsettings,
                    stoppingToken);

                password = Password(10,
                    "Password",
                    $"Personal Access Token from {origin} Azure-Devops",
                    stoppingToken);


                var azbase = AzureDevopsPrefix(uribase, prefixfeed,_logger);
                var aux = ReadFeeds(httpClient,
                    azbase!,
                    password,
                    "Trying retrieve list of feeds...", "",
                    stoppingToken);

                if (aux.Exception != null)
                {
                    _logger?.LogError($"Error retrieve list of feeds: {aux.Exception}");
                    var tryanother = Question("Try again", aux.Exception.Message, true, stoppingToken);
                    _logger?.LogInformation($"Try again: {tryanother}");
                    if (!tryanother)
                    {
                        ExitTanks(0);
                    }
                    continue;
                }
                if (aux.value.Length == 0 && AppConstants.IsOriSource(origin))
                {
                    var msg = "Found 0 feeds!. The application will be closed";
                    _logger?.LogInformation(msg);
                    PromptPlus.WriteLine(msg, PromptPlus.ColorSchema.Error);
                    PromptPlus.KeyPress().Run(stoppingToken);
                    ExitTanks(0);
                }
                feeds = aux.value;

                seletedfeed = SelectAzureFeed(feeds, defvalue, "Select a feed", "", defaultsettings, stoppingToken);

                var protocols = usersettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries);

                if (seletedfeed.upstreamSources.Any(x => protocols.Select(x => x.ToLowerInvariant()).Contains(x.protocol.ToLowerInvariant())))
                {
                    var unaccepted = Question(
                        $"Filter Unaccepted Upstream ({seletedfeed.upstreamSources.Count()})?",
                        "",
                        true,
                        stoppingToken);
                    _logger?.LogInformation($"Filter Unaccepted Upstream: {unaccepted}");
                    if (unaccepted)
                    {
                        denyupstream = UpstreamSource(seletedfeed.upstreamSources, "Choose Unaccepted Upstream Sources", "", stoppingToken);
                    }

                }
                packageread = new ProcessReadPackges();
                foreach (var item in protocols)
                {
                    string readsource;
                    if (seletedfeed.project == null)
                    {
                        readsource = ProtocolsTransferConstant.UriPackageList
                            .Replace("{baseorg}", AzureDevopsPrefix(uribase, prefixpkg, _logger), StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{feedname}", seletedfeed.name, StringComparison.InvariantCultureIgnoreCase)
                            .Replace("{filtertype}", item, StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        readsource = ProtocolsTransferConstant.UriPackageScopedList
                            .Replace("{baseorg}", AzureDevopsPrefix(uribase, prefixpkg, _logger), StringComparison.InvariantCultureIgnoreCase)
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

                    var itempackageread = AzureReadPackges(item,defsufix, denyupstream,
                        httpClient,
                        defaultsettings.takequery,
                        readsource,
                        password,
                        $"Reading packages {item}", "", stoppingToken);

                    var newpkg = packageread.Packages.ToList();
                    newpkg.AddRange(itempackageread.Packages);

                    packageread.DistinctQtd += itempackageread.DistinctQtd;
                    packageread.Packages = newpkg.ToArray();
                }

                break;
            }

            _logger?.LogInformation($"AzureReadPackges {origin} uribase : {uribase}");
            _logger?.LogInformation($"AzureReadPackges {origin} prefix feed: {prefixfeed}");
            _logger?.LogInformation($"AzureReadPackges {origin} prefix pkg : {prefixfeed}");
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
                Uribase = uribase,
                Prefixurifeed = prefixfeed,
                Prefixuripkg = prefixfeed,
                Passsword = password,
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
                        return result.Uri.AbsoluteUri.Substring(0, result.Uri.AbsoluteUri.Length - 1);
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
