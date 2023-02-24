using PackagesTransfer.Models;
using PackagesTransfer.Prompts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PPlus;

namespace PackagesTransfer;
internal class MainProgram : IHostedService
{
    private readonly Defaultsettings _defaultsettings;
    private readonly PromptTransfer _promptTransfer;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly HttpClient _httpClientAzure;
    private readonly CancellationToken _appstoptoken;

    private readonly ILogger<MainProgram> _logger;

    private StatusProtocols _statusProtocols;
    private Usersettings _lastsettings;
    private FeedDataInfo _sourceinfo;
    private FeedDataInfo _targetinfo;
    private Task? _maintask;

    public MainProgram(ILogger<MainProgram> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, PromptTransfer promptTransfer, IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _appstoptoken = _appLifetime.ApplicationStopping;
        _defaultsettings = configuration.GetSection("Defaultsettings")!.Get<Defaultsettings>()!;

        _promptTransfer = promptTransfer;
        _httpClientAzure = httpClientFactory.CreateClient(AppConstants.HttpClientNameAzure);

        _statusProtocols = new StatusProtocols();
        _sourceinfo = new FeedDataInfo();
        _targetinfo = new FeedDataInfo();
        _lastsettings = new Usersettings
        {
            feedsource = _defaultsettings.sourceuri,
            feedtarget = _defaultsettings.targeturi,
            filterProtocoltype = _defaultsettings.protocols,
            tmpfolder = _defaultsettings.tmpfolder,
            prefixurifeedsource = _defaultsettings.prefixurifeedsource,
            prefixurifeedtarget = _defaultsettings.prefixurifeedtarget,
            prefixuripkgsource = _defaultsettings.prefixuripkgsource,
            prefixuripkgtarget = _defaultsettings.prefixuripkgtarget,
            sourceuri = _defaultsettings.sourceuri,
            targeturi = _defaultsettings.targeturi
        };
    }

    public Task StartAsync(CancellationToken cancellation)
    {
        _maintask = Task.Run(async () =>
        {
            //PromptPlus Global setting
            PromptPlus.HideSymbolPromptAndDone = true;
            PromptPlus.EnabledAbortKey = false;
            PromptPlus.HideAfterFinish = true;
            PromptPlus.EnabledTooltip = false;

            try
            {
                _logger?.LogInformation("Started FeedTransferPlus");

                _promptTransfer.ShowTitle();

                _statusProtocols = _promptTransfer.CheckProtocols(_defaultsettings.timeoutcmd, _appstoptoken);

                PromptPlus.WriteLine($"Found Nuget protocol({_statusProtocols.NugetVersion ?? string.Empty}). This tool has buit-in protocol", PromptPlus.ColorSchema.Answer);
                if (!_statusProtocols.EnabledNpm)
                {
                    PromptPlus.WriteLine($"For Npm protocol, you must have the corresponding client version installed on your machine\n", PromptPlus.ColorSchema.Error);
                }
                else
                {
                    PromptPlus.WriteLine($"Found Npm protocol({_statusProtocols.NpmVersion ?? string.Empty}). This tool installed on your machine", PromptPlus.ColorSchema.Answer);
                    PromptPlus.WriteLine($"Found Npm user file: {_statusProtocols.NpmUserFileLocation ?? string.Empty}\n", PromptPlus.ColorSchema.Answer);
                }

                _lastsettings = _promptTransfer.UserSettings(_appstoptoken);

                var rerun = false;
                do
                {
                    if (rerun)
                    {
                        PromptPlus.Banner("Feed Transfer")
                            .Run(ConsoleColor.Green);
                    }
                    _logger?.LogInformation("Phase 0");
                    PromptPlus.WriteLine("\nSelect Type Transfer", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("====================\n", forecolor: ConsoleColor.White);

                    var oldtype = _lastsettings.typetransfer;

                    _lastsettings.typetransfer = _promptTransfer
                        .TypeTransfer(rerun,_lastsettings, _appstoptoken);

                    AppConstants.SelectedTypeTransfer(_lastsettings.typetransfer);

                    var newtype = _lastsettings.typetransfer;

                    if (oldtype != newtype)
                    {
                        _lastsettings = ResetUserSettings(newtype);
                        _lastsettings.typetransfer = newtype;
                    }

                    _logger?.LogInformation("Phase 1");
                    PromptPlus.WriteLine("\nSelect protocol types", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("=====================\n", forecolor: ConsoleColor.White);

                    _lastsettings.filterProtocoltype = _promptTransfer
                        .ProtocolType(rerun,_lastsettings, _statusProtocols, _appstoptoken);

                    var protocols = _lastsettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries);

                    _logger?.LogInformation("Phase 2");
                    PromptPlus.WriteLine("\nSource Information", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("==================\n", forecolor: ConsoleColor.White);

                    var tryaotherfeed = false;
                    while (true)
                    {
                        if (AppConstants.IsBetweenAzuredevops || AppConstants.IsAzuredevopsToFile)
                        {
                            if (rerun || tryaotherfeed)
                            {
                                _sourceinfo = _promptTransfer.ReRunSelectAzureDevopsOrigin(_sourceinfo, _httpClientAzure,
                                    AppConstants.NameOriSource, _lastsettings, _defaultsettings, _appstoptoken);
                            }
                            else
                            {
                                _sourceinfo = _promptTransfer.SelectAzureDevopsOrigin(_httpClientAzure,
                                    AppConstants.NameOriSource, _lastsettings, _defaultsettings, _appstoptoken);
                            }
                        }
                        else if (AppConstants.IsFileToAzuredevops)
                        {
                            var defpatrh = string.Empty;
                            if (rerun || tryaotherfeed)
                            {
                                defpatrh = Path.Combine(_sourceinfo.Uribase, _sourceinfo.Seleted!.name!);
                            }
                            else
                            {
                                defpatrh = Path.Combine(_lastsettings.sourceuri, _lastsettings.feedsource);
                            }
                            _sourceinfo = _promptTransfer.SelectPathOrigin(AppConstants.NameOriSource, defpatrh, _lastsettings, _defaultsettings, _appstoptoken);
                        }
                        else
                        {
                            var msg = "Invalid type transfer in Get information from source";
                            _logger?.LogError(msg);
                            throw new Exception(msg);
                        }

                        if (_sourceinfo.Packages!.Count() == 0)
                        {
                            tryaotherfeed = _promptTransfer.Question("Found 0 packages!. Try another feed?", "", true, _appstoptoken);
                            if (!tryaotherfeed)
                            {
                                _promptTransfer.ExitTanks(0);
                            }
                            continue;
                        }
                        break;
                    }

                    _lastsettings.sourceuri = _sourceinfo.Uribase;
                    _lastsettings.feedsource = _sourceinfo.Seleted!.name!;
                    _lastsettings.prefixurifeedsource = _sourceinfo.Prefixurifeed;
                    _lastsettings.prefixuripkgsource = _sourceinfo.Prefixuripkg;

                    PromptPlus.WriteLine($"Source : {_sourceinfo.Uribase}", ConsoleColor.Yellow);
                    PromptPlus.WriteLine($"Feed Selected: {_sourceinfo.Seleted!.name}", ConsoleColor.Yellow);
                    PromptPlus.WriteLine($"Found {_sourceinfo.DistinctPackageCount} different {_lastsettings.filterProtocoltype} items", ConsoleColor.Yellow);
                    foreach (var item in protocols)
                    {
                        PromptPlus.WriteLine($"Total {item}, counting all version: {_sourceinfo.Packages!.Where(x => x.Protocol == item).Count()}", ConsoleColor.Yellow); 
                    }

                    _logger?.LogInformation("Phase 3");
                    PromptPlus.WriteLine("\nTarget information", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("==================\n", forecolor: ConsoleColor.White);

                    if (AppConstants.IsBetweenAzuredevops || AppConstants.IsFileToAzuredevops)
                    {
                        if (rerun)
                        {
                            _targetinfo = _promptTransfer.ReRunSelectAzureDevopsOrigin(_targetinfo, _httpClientAzure,
                                AppConstants.NameOriTarget, _lastsettings, _defaultsettings, _appstoptoken);
                        }
                        else
                        {
                            _targetinfo = _promptTransfer.SelectAzureDevopsOrigin(_httpClientAzure,
                                AppConstants.NameOriTarget, _lastsettings, _defaultsettings, _appstoptoken);
                        }
                    }
                    else if (AppConstants.IsAzuredevopsToFile)
                    {
                        var defpatrh = string.Empty;
                        if (rerun)
                        {
                            defpatrh = Path.Combine(_targetinfo.Uribase, _targetinfo.Seleted!.name!);
                        }
                        _targetinfo = _promptTransfer.SelectPathOrigin(AppConstants.NameOriTarget,defpatrh, _lastsettings, _defaultsettings, _appstoptoken);
                    }
                    else
                    {
                        var msg = "Invalid type transfer in Get information from target";
                        _logger?.LogError(msg);
                        throw new Exception(msg);
                    }

                    _lastsettings.targeturi = _targetinfo.Uribase;
                    _lastsettings.feedtarget = _targetinfo.Seleted!.name!;
                    _lastsettings.prefixurifeedtarget = _targetinfo.Prefixurifeed;
                    _lastsettings.prefixuripkgtarget = _targetinfo.Prefixuripkg;

                    PromptPlus.WriteLine($"Target : {_targetinfo.Uribase}", ConsoleColor.Yellow);
                    PromptPlus.WriteLine($"Feed Selected: {_targetinfo.Seleted!.name}", ConsoleColor.Yellow);
                    PromptPlus.WriteLine($"Found {_targetinfo.DistinctPackageCount} different {_lastsettings.filterProtocoltype} items", ConsoleColor.Yellow);
                    foreach (var item in protocols)
                    {
                        PromptPlus.WriteLine($"Total {item}, counting all version: {_targetinfo.Packages!.Where(x => x.Protocol == item).Count()}", ConsoleColor.Yellow);
                    }

                    _logger?.LogInformation("Phase 4");
                    PromptPlus.WriteLine("\nFind already published", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("======================\n", forecolor: ConsoleColor.White);
                    var qtdtransfer = NormalizeExport();

                    _logger?.LogInformation("Phase 5");
                    PromptPlus.WriteLine("\nTransfer source items to target", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("===============================\n", forecolor: ConsoleColor.White);

                    var result = await _promptTransfer.TransferPackages(_httpClientAzure, _sourceinfo, _targetinfo, _lastsettings, _defaultsettings, _appstoptoken);

                    _lastsettings.tmpfolder = result.TempFolder;
                    _targetinfo.DistinctPackageCount += result.NewDistinctQtd;
                    var auxnew = _targetinfo.Packages!.ToList();
                    auxnew.AddRange(result.PackagesOk);
                    _targetinfo.Packages = auxnew.ToArray();

                    PromptPlus.WriteLine($"Total elapsed timing : {FormatTimeSpan(result.ElapsedTime)}", ConsoleColor.Yellow);

                    _logger?.LogInformation("Phase 6");
                    PromptPlus.WriteLine("\nResume Packages Transfer", forecolor: ConsoleColor.White);
                    PromptPlus.WriteLine("========================\n", forecolor: ConsoleColor.White);
                    rerun = _promptTransfer.ResumeTransfer(result, _lastsettings, _defaultsettings, _appstoptoken);
                    _logger?.LogInformation($"Rerum FeedTransfer : {rerun}");

                } while (rerun);

                _promptTransfer.ExitTanks();
                PromptPlus.KeyPress().Run();
                _appLifetime.StopApplication();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"General error: {ex}");
                PromptPlus.WriteLine(ex, PromptPlus.ColorSchema.Error);
                _promptTransfer.ExitTanks(1);
            }
        }, cancellation);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger?.LogInformation($"Stoped Application");
        if (_maintask != null)
        {
            await _maintask;
            _maintask.Dispose();
        }
    }

    private Usersettings ResetUserSettings(string newtypetransfer)
    {
        var defsrc = string.Empty;
        var deftrg = string.Empty;
        var defprefixfedsrc = string.Empty;
        var defprefixpkgsrc = string.Empty;
        var defprefixfedtrg = string.Empty;
        var defprefixpkgtrg = string.Empty;
        if (AppConstants.IsBetweenAzuredevops)
        {
            defsrc = _defaultsettings.sourceuri;
            deftrg = _defaultsettings.targeturi;
            defprefixfedsrc = _defaultsettings.prefixurifeedsource;
            defprefixpkgsrc = _defaultsettings.prefixuripkgsource;
            defprefixfedtrg = _defaultsettings.prefixurifeedtarget;
            defprefixpkgtrg = _defaultsettings.prefixuripkgtarget;
        }
        else if (AppConstants.IsAzuredevopsToFile)
        {
            defsrc = _defaultsettings.sourceuri;
            defprefixfedsrc = _defaultsettings.prefixurifeedsource;
            defprefixpkgsrc = _defaultsettings.prefixuripkgsource;
        }
        else if (AppConstants.IsFileToAzuredevops)
        {
            deftrg = _defaultsettings.targeturi;
            defprefixfedtrg = _defaultsettings.prefixurifeedtarget;
            defprefixpkgtrg = _defaultsettings.prefixuripkgtarget;
        }

        return new Usersettings
        {
            feedsource = "",
            feedtarget = "",
            filterProtocoltype = _defaultsettings.protocols,
            tmpfolder = _defaultsettings.tmpfolder,
            prefixurifeedsource = defprefixfedsrc,
            prefixurifeedtarget = defprefixfedtrg,
            prefixuripkgsource = defprefixpkgsrc,
            prefixuripkgtarget = defprefixpkgtrg,
            sourceuri = defsrc,
            targeturi = deftrg
        };
    }

    private int NormalizeExport()
    {
        if (_sourceinfo.Packages == null)
        {
            var msg = "Invalid type packages in source";
            _logger?.LogError(msg);
            throw new Exception(msg);
        }
        if (_targetinfo.Packages == null)
        {
            var msg = "Invalid type packages in target";
            _logger?.LogError(msg);
            throw new Exception(msg);
        }
        int qtddup = 0; 
        var protocols = _lastsettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var dupaux = new List<PackageInfo>();
        foreach (var itemprotocol in protocols)
        {
            List<PackageInfo> aux = _sourceinfo.Packages
                .Where(x => x.Protocol == itemprotocol).ToList();
            foreach (PackageInfo item in _targetinfo.Packages.Where(x => x.Protocol == itemprotocol))
            {
                var pos = aux.FindIndex(x => x.Id.ToLower() == item.Id.ToLower() && x.Version.ToLower() == item.Version.ToLower());
                if (pos >= 0)
                {
                    qtddup++;
                    aux.RemoveAt(pos);
                }
            }
            dupaux.AddRange(aux);
        }
        _sourceinfo.Packages = dupaux.ToArray();
        
        _logger.LogInformation($"Removed {qtddup} items. After checked : {_sourceinfo.Packages.Length}");

        PromptPlus.WriteLine($"Removed {qtddup} items already published on target", ConsoleColor.Yellow);
        PromptPlus.WriteLine($"Total items after checked : {_sourceinfo.Packages.Length}", ConsoleColor.Yellow);
        foreach (var item in protocols)
        {
            PromptPlus.WriteLine($"Total {item}, counting all version: {_sourceinfo.Packages!.Where(x => x.Protocol == item).Count()}", ConsoleColor.Yellow);
        }
        return _sourceinfo.Packages.Length;
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        static string tupleFormatter(Tuple<int, string> t) => $"{t.Item1} {t.Item2}";
        List<Tuple<int, string>> components = new()
        {
            Tuple.Create(timeSpan.Hours,timeSpan.Hours<=1?"hour":"hours"),
            Tuple.Create(timeSpan.Minutes,timeSpan.Minutes<=1?"minute":"minutes"),
            Tuple.Create(timeSpan.Seconds,timeSpan.Seconds<=1?"second":"seconds"),
        };

        components.RemoveAll(i => i.Item1 == 0);

        string extra = "";

        if (components.Count > 1)
        {
            Tuple<int, string> finalComponent = components[components.Count - 1];
            components.RemoveAt(components.Count - 1);
            extra = $" and {finalComponent.Item1} {finalComponent.Item2}";
        }
        return $"{string.Join(", ", components.Select(tupleFormatter))}{extra}";
    }

  }
