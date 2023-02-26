using PPlus.Objects;
using PPlus;
using PackagesTransfer.Models;
using PackagesTransfer.Protocols;
using Microsoft.Extensions.Logging;
using PackagesTransfer.Protocols.Npm;
using PackagesTransfer.Protocols.Nuget;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public FeedDataInfo SelectPathOrigin(string origin, string pathdefault, Usersettings usersettings, Defaultsettings defaultsettings, CancellationToken stoppingToken)
        {
            ResultBrowser folder;
            PackageInfo[] packages = Array.Empty<PackageInfo>();
            var distintIdpkg = new List<string>();
            if (string.IsNullOrEmpty(pathdefault))
            {
                pathdefault = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
            }
            while (true)
            {
                var dryfolder = 0;
                if (AppConstants.IsOriSource(origin))
                {
                    var infobrowser = PromptPlus
                        .Browser($"Select folder {origin} for items {usersettings.filterProtocoltype}")
                        .AllowNotSelected(false)
                        .Default(pathdefault)
                        .Filter(BrowserFilter.OnlyFolder)
                        .PageSize(defaultsettings.pagelength)
                        .Run(stoppingToken);
                    if (infobrowser.IsAborted)
                    {
                        ExitTanks(1);
                    }
                    var subfolders = HasSubfolders(Path.Combine(infobrowser.Value.PathValue, infobrowser.Value.SelectedValue));
                    if (subfolders > 0)
                    {
                        var qtddry = PromptPlus.SliderNumber(SliderNumberType.LeftRight, $"How many levels do you want to read ahead", $"The folder has {subfolders} sub-folders(Level 1).Zero value does not read any subfolders")
                            .Range(0, 10)
                            .FracionalDig(0)
                            .Step(1)
                            .LargeStep(5)
                            .Run(stoppingToken);
                        if (qtddry.IsAborted)
                        {
                            ExitTanks(1);
                        }
                        dryfolder = (int)qtddry.Value;
                    }
                    folder = infobrowser.Value;
                }
                else if (AppConstants.IsOriTarget(origin))
                {
                    var infobrowser = PromptPlus
                        .Browser($"Select destination folder for items {usersettings.filterProtocoltype}")
                        .AllowNotSelected(true)
                        .Default(pathdefault)
                        .Filter(BrowserFilter.OnlyFolder)
                        .PageSize(defaultsettings.pagelength)
                        .Run(stoppingToken);
                    if (infobrowser.IsAborted)
                    {
                        ExitTanks(1);
                    }
                    var selpath = infobrowser.Value.PathValue;
                    if (string.IsNullOrEmpty(selpath))
                    {
                        selpath = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
                    }
                    if (infobrowser.Value.NotFound)
                    {
                        Directory.CreateDirectory(Path.Combine(infobrowser.Value.PathValue, infobrowser.Value.SelectedValue));
                    }
                    folder = infobrowser.Value;
                }
                else
                {
                    var msg = $"Invalid origin{origin} in SelectPathOrigint";
                    _logger?.LogError(msg);
                    throw new NotImplementedException(msg);
                }
                var extprotocol = "";
                foreach (var item in usersettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (item == ProtocolsTransferConstant.NameNugetProtocol)
                    {
                        foreach (var sufixitem in defaultsettings.sufixnuget.Split(";", StringSplitOptions.RemoveEmptyEntries))
                        {
                            extprotocol += "*" + sufixitem + ";";

                        }
                    }
                    else if (item == ProtocolsTransferConstant.NameNpmProtocol)
                    {
                        foreach (var sufixitem in defaultsettings.sufixnpm.Split(";", StringSplitOptions.RemoveEmptyEntries))
                        {
                            extprotocol += "*" + sufixitem + ";";

                        }
                    }
                }
                if (extprotocol.Length == 0)
                {
                    extprotocol = "*.*";
                }
                if (extprotocol.EndsWith(";"))
                {
                    extprotocol = extprotocol.Substring(0, extprotocol.Length - 1); 
                }
                var auxitems = new List<string>();
                foreach (var itemprotocol in extprotocol.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var auxfiles = Directory.GetFiles(Path.Combine(folder.PathValue, folder.SelectedValue), itemprotocol).ToList();
                    if (dryfolder > 0)
                    {
                        IEnumerable<string> namessubfolders = LoadSubDirs(dryfolder, Path.Combine(folder.PathValue, folder.SelectedValue));
                        foreach (string subfolder in namessubfolders)
                        {
                            var itemssub = Directory.GetFiles(Path.Combine(subfolder), itemprotocol);
                            foreach (var item in itemssub)
                            {
                                auxfiles.Add(item);
                            }
                        }
                    }
                    auxitems.AddRange(auxfiles);
                }
                var items = auxitems.ToArray();
                if (AppConstants.IsOriTarget(origin))
                {
                    if (items.Length > 0)
                    {
                        var tryanother = Question($"The folder {folder.SelectedValue} has {items.Length} files. Confirm?",
                            $"Files EndsWith: {extprotocol}", true, stoppingToken);
                        if (!tryanother)
                        {
                            continue;
                        }
                    }
                }

                var aux = new List<PackageInfo>();
                if (items.Length > 0)
                {
                    ResultPromptPlus<IEnumerable<ResultProcess>> process = PromptPlus.WaitProcess($"Reading {items.Length} items(find and validating packages...)", "")
                        .AddProcess(new SingleProcess(async (StopApp) =>
                        {
                            foreach (var item in items)
                            {
                                try
                                {
                                    if (usersettings.filterProtocoltype.Contains(ProtocolsTransferConstant.NameNugetProtocol) && IsValidSufix(item,defaultsettings.sufixnuget))
                                    {
                                        try
                                        {
                                            var auxpkg = NugetHelpper.ExtractInfoFromfile(item);
                                            if (auxpkg != null)
                                            {
                                                aux.Add(auxpkg);
                                                if (!distintIdpkg.Any(x => x == $"{auxpkg.Id}{ProtocolsTransferConstant.NameNugetProtocol}"))
                                                {
                                                    distintIdpkg.Add($"{auxpkg.Id}{ProtocolsTransferConstant.NameNugetProtocol}");
                                                }
                                            }
                                            else
                                            {
                                                _logger?.LogError($"Invalid {ProtocolsTransferConstant.NameNugetProtocol} {item}. Not found ID or Version");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogError($"Invalid {ProtocolsTransferConstant.NameNugetProtocol} {item}. {ex}");
                                        }

                                    }
                                    if (usersettings.filterProtocoltype.Contains(ProtocolsTransferConstant.NameNpmProtocol) && IsValidSufix(item, defaultsettings.sufixnpm))
                                    {
                                        try
                                        {
                                            var auxpkg = NpmCommands.ExtractInfoFromfile(item,_logger);
                                            if (auxpkg != null)
                                            {
                                                aux.Add(auxpkg);
                                                if (!distintIdpkg.Any(x => x == $"{auxpkg.Id}{ProtocolsTransferConstant.NameNpmProtocol}"))
                                                {
                                                    distintIdpkg.Add($"{auxpkg.Id}{ProtocolsTransferConstant.NameNpmProtocol}");
                                                }
                                            }
                                            else
                                            {
                                                _logger?.LogError($"Invalid {ProtocolsTransferConstant.NameNpmProtocol} {item}. Not found name or Version");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogError($"Invalid {ProtocolsTransferConstant.NameNpmProtocol} {item}. {ex}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogError($"Invalid {item}: {ex}");
                                }
                            }
                            return await Task.FromResult<object>("");
                        }, processTextResult: (x) => ""))
                        .Run(stoppingToken);

                    if (process.IsAborted)
                    {
                        ExitTanks(1);
                    }
                }
                if (aux.Count == 0 && AppConstants.IsOriSource(origin))
                {
                    var tryanother = Question($"Try with another {origin} folder", $"Found 0 item {usersettings.filterProtocoltype}", true, stoppingToken);
                    if (!tryanother)
                    {
                        ExitTanks(0);
                    }
                    continue;
                }
                packages = aux.ToArray();
                break;
            }

            _logger?.LogInformation($"AzureReadPackges {origin} uribase : {folder.PathValue}");
            _logger?.LogInformation($"AzureReadPackges {origin} prefix feed: ");
            _logger?.LogInformation($"AzureReadPackges {origin} prefix pkg : ");
            _logger?.LogInformation($"AzureReadPackges {origin} Feed: {folder.SelectedValue}");
            _logger?.LogInformation($"AzureReadPackges Distinct Package Count: {distintIdpkg.Count}");
            foreach (var item in packages)
            {
                _logger?.LogInformation($"Packges: {item.Id} Ver:{item.Version} Protocol: {item.Protocol}");
            }

            return new FeedDataInfo
            {
                Uribase = folder.PathValue,
                Prefixurifeed = "",
                Prefixuripkg = "",
                Passsword = "",
                Feeds = Array.Empty<FeedValue>(),
                Seleted = new FeedValue { name = folder.SelectedValue },
                DistinctPackageCount = distintIdpkg.Count,
                Packages = packages
            };

            static int HasSubfolders(string path)
            {
                IEnumerable<string> subfolders = Directory.EnumerateDirectories(path);
                return subfolders.Count();
            }

            static bool IsValidSufix(string file, string sufixs)
            {
                return sufixs
                    .Split(";", StringSplitOptions.RemoveEmptyEntries)
                    .Any(x => file.ToLower().EndsWith(x.ToLower()));
            }

            static IEnumerable<string> LoadSubDirs(int deep, string path)
            {
                var subdirectoryEntries = Directory.EnumerateDirectories(path).ToList();
                foreach (string subdirectory in Directory.EnumerateDirectories(path))
                {
                    if (deep < 0)
                    {
                        break;
                    }
                    subdirectoryEntries.AddRange(LoadSubDirs(deep - 1, subdirectory));
                }
                return subdirectoryEntries;
            }

        }
    }
}
