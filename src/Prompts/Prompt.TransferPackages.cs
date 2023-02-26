using PPlus.Objects;
using PPlus;
using System.Text;
using PackagesTransfer.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using System.Net.Http.Headers;
using System.Net;
using NuGet.Protocol;
using PackagesTransfer.Protocols;
using System.Web;
using PackagesTransfer.Protocols.Npm;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public async Task<ProcessTransfer> TransferPackages(HttpClient httpClient, FeedDataInfo source, FeedDataInfo target, Usersettings usersettings, Defaultsettings defaultsettings, CancellationToken stoppingToken)
        {
            var IsEmptyUserCredential = false;
            List<PackageInfo> publishsource;
            string publishprotocol;
            string? npmregistry;

            if (source.Packages == null)
            {
                var msg = "TransferPackages, Invalid type packages in source";
                _logger?.LogError(msg);
                throw new ArgumentException(msg);
            }
            if (target.Packages == null)
            {
                var msg = "TransferPackages, Invalid type packages in target";
                _logger?.LogError(msg);
                throw new ArgumentException(msg);
            }

            var localfolder = usersettings.tmpfolder;
            if (AppConstants.IsBetweenAzuredevops)
            {
                localfolder = TempFolder("FolderExportPackages", 3,
                    "Temporary local Folder to download: ",
                    "The folder will be removed(If created) at the end",
                    usersettings.tmpfolder,
                    defaultsettings,
                    stoppingToken);
            }
            if (!Question($"Confirms the transfer of {source.Packages.Length} items", "", true, stoppingToken))
            {
                _logger.LogInformation("TransferPackages, Not Confirms the export");
                return new ProcessTransfer() { TempFolder = localfolder };
            }

            var destfolder = localfolder;
            var msgprgbar = $"Download to local folder and then publish to target. Press <ESC> to abort transfer";
            bool created = false;
            if (AppConstants.IsFileToAzuredevops)
            {
                var srcfolder = Path.Combine(source.Uribase, source.Seleted!.name!);
                msgprgbar = $"Read from local folder and then publish to target. Press <ESC> to abort transfer";
            }
            else if (AppConstants.IsBetweenAzuredevops)
            {

                try
                {
                    if (!Directory.Exists(destfolder!))
                    {
                        created = true;
                        Directory.CreateDirectory(destfolder);
                    }
                    _logger.LogInformation($"TransferPackages, Cretated local folder :{destfolder}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"TransferPackages, Error Cretated local folder {destfolder}: {ex}");
                    throw;
                }
            }
            else if (AppConstants.IsAzuredevopsToFile)
            {
                destfolder = Path.Combine(target.Uribase, target.Seleted!.name!);
                msgprgbar = $"Download to local folder. Press <ESC> to abort transfer";
            }
            else
            {
                _logger.LogError("TransferPackages, Invalid type transfer");
                throw new NotImplementedException("Invalid type transfer");
            }

            var resumePublishOk = new List<PackageInfo>();
            var resumePublishErr = new List<PackageInfo>();
            List<string> newItemExport = new();
            Stopwatch sw = new();
            var hasErrorUserconfigNpm = false;
            string? msgerrprocess = null;
            try
            {
                sw.Start();
                var protocols = usersettings.filterProtocoltype.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var result = new List<PackageInfo>();
                var canceltransfer = false;
                foreach (var itemprotocol in protocols)
                {
                    npmregistry = null;
                    publishprotocol = itemprotocol;
                    try
                    {
                        if (publishprotocol == ProtocolsTransferConstant.NameNpmProtocol)
                        {
                            var cmd = await NpmCommands.SetUserConfigTransfer(target, defaultsettings.timeoutcmd, _logger, stoppingToken);
                            if (cmd.Error != null)
                            {
                                hasErrorUserconfigNpm = true;
                                publishsource = source.Packages.Where(x => x.Protocol == itemprotocol).ToList();
                                foreach (var item in publishsource)
                                {
                                    item.ResumeInfo = cmd.Error;
                                }
                                result.AddRange(publishsource);
                                continue;
                            }
                            npmregistry = cmd.UriRegistry!;
                        }
                        publishsource = source.Packages.Where(x => x.Protocol == itemprotocol).ToList();
                        result.AddRange(publishsource);
                        var prgbar = PromptPlus
                            .Progressbar($"Transfering {itemprotocol} items.", msgprgbar)
                            .Width(100)
                            .Config(x => x.EnabledAbortKey(true))
                            .StartInterationId(0)
                            .UpdateHandler(DownloadAndPublishNuget)
                            .Run(stoppingToken);
                        
                        canceltransfer = prgbar.IsAborted;
                    }
                    finally
                    {
                        if (!hasErrorUserconfigNpm && publishprotocol == ProtocolsTransferConstant.NameNpmProtocol)
                        {

                            var cmd = await NpmCommands.ResetUserConfig(defaultsettings.timeoutcmd, _logger, stoppingToken);
                            if (cmd.Error != null)
                            {
                                msgerrprocess = cmd.Error;
                            }
                        }
                    }
                    if (canceltransfer)
                    {
                        break;
                    }
                }
                source.Packages = result.ToArray();
            }
            finally
            {
                if (created)
                {
                    try
                    {
                        foreach (string item in Directory.EnumerateFiles(destfolder))
                        {
                            File.Delete(item);
                        }
                        Directory.Delete(destfolder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"TransferPackages erro removed files and folder: {destfolder}: {ex}");
                    }
                }
                sw.Stop();
            }
            if (!string.IsNullOrEmpty(msgerrprocess))
            {
                throw new InvalidOperationException(msgerrprocess);
            }
            _logger.LogInformation($"Local Folder : {destfolder}");
            _logger.LogInformation($"Elapsed TimeLocal : {sw.Elapsed}");
            _logger.LogInformation($"New Distinct packages Qtd. : {newItemExport.Count}");
            foreach (var item in resumePublishOk)
            {
                _logger.LogInformation($"Publish packages : {item.Id} Version: {item.Version}");

            }
            foreach (var item in resumePublishErr)
            {
                _logger.LogInformation($"Publish Error packages : {item.Id} Version: {item.Version} Err: {item.ResumeInfo??string.Empty}");
            }

            return new ProcessTransfer()
            {
                TempFolder = destfolder,
                ElapsedTime = sw.Elapsed,
                NewDistinctQtd = newItemExport.Count,
                PackagesErr = resumePublishErr.ToArray(),
                PackagesOk = resumePublishOk.ToArray()
            };

            static string AzureDevopsPrefix(string value, string prefix)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    if (value.EndsWith("/"))
                    {
                        return value[..^1];
                    }
                    return value;
                }
                Uri.TryCreate(value, UriKind.Absolute, out Uri? uriinput);
                UriBuilder result = new(uriinput!.Scheme, $"{prefix}.{uriinput.Authority}", uriinput.Port, uriinput.PathAndQuery);
                if (result.Uri.AbsoluteUri.EndsWith("/"))
                {
                    return result.Uri.AbsoluteUri.Substring(0, result.Uri.AbsoluteUri.Length - 1);
                }
                return result.Uri.AbsoluteUri;
            }

            async Task<ProgressBarInfo> DownloadAndPublishNuget(ProgressBarInfo info, CancellationToken cancellationToken)
            {
                if (publishsource.Count == 0)
                {
                    return new ProgressBarInfo(100, true, $"(0/0) ", 0);
                }
                int index = (int)info.InterationId;
                int perc = index * 100 / publishsource.Count;
                PackageInfo pkg = publishsource[index];

                var filenameToDownload = pkg.FileName!.Replace("@", "");
                if (filenameToDownload.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    foreach (var item in Path.GetInvalidFileNameChars())
                    {
                        filenameToDownload = filenameToDownload.Replace(item, '.');
                    }
                }
                string pathdownload = Path.Combine(destfolder,filenameToDownload);

                if (AppConstants.IsAzuredevopsToFile)
                {
                    pathdownload = Path.Combine(target.Uribase, target.Seleted!.name!, $"{filenameToDownload}");
                }
                if (AppConstants.IsFileToAzuredevops)
                {
                    pathdownload = Path.Combine(source.Uribase, source.Seleted!.name!, $"{filenameToDownload}");
                }

                pkg.ResumeInfo = string.Empty;
                var hascopy = AppConstants.IsFileToAzuredevops;
                if (!hascopy)
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(
                            Encoding.ASCII.GetBytes(
                                string.Format("{0}:{1}", "", source.Passsword))));

                    string uridownload = string.Empty;
                    if (publishprotocol == ProtocolsTransferConstant.NameNugetProtocol)
                    {
                        if (source.Seleted!.project != null && source.Seleted!.project.name! != null)
                        {
                            uridownload = ProtocolsTransferConstant.UriNugetPackageScopedDownload
                                .Replace("{baseorg}", AzureDevopsPrefix(source.Uribase, source.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{feedname}", HttpUtility.UrlEncode(source.Seleted!.name!), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{projectname}", HttpUtility.UrlEncode(source.Seleted!.project!.name!), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgname}", HttpUtility.UrlEncode(pkg.Id), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgver}", HttpUtility.UrlEncode(pkg.Version), StringComparison.InvariantCultureIgnoreCase);
                        }
                        else
                        {
                            uridownload = ProtocolsTransferConstant.UriNugetPackageDownload
                                .Replace("{baseorg}", AzureDevopsPrefix(source.Uribase, source.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{feedname}", HttpUtility.UrlEncode(source.Seleted!.name!), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgname}", HttpUtility.UrlEncode(pkg.Id), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgver}", HttpUtility.UrlEncode(pkg.Version), StringComparison.InvariantCultureIgnoreCase);
                        }
                    }
                    if (publishprotocol == ProtocolsTransferConstant.NameNpmProtocol)
                    {
                        if (source.Seleted!.project != null && source.Seleted!.project!.name! != null)
                        {
                            uridownload = ProtocolsTransferConstant.UriNmpPackageScopedDownload
                                .Replace("{baseorg}", AzureDevopsPrefix(source.Uribase, source.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{feedname}", HttpUtility.UrlEncode(source.Seleted!.name!), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{projectname}", HttpUtility.UrlEncode(source.Seleted!.project!.name!), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgname}", HttpUtility.UrlEncode(pkg.Id), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgver}", HttpUtility.UrlEncode(pkg.Version), StringComparison.InvariantCultureIgnoreCase);
                        }
                        else
                        {
                            uridownload = ProtocolsTransferConstant.UriNmpPackageDownload
                                .Replace("{baseorg}", AzureDevopsPrefix(source.Uribase, source.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{feedname}", HttpUtility.UrlEncode(source.Seleted!.name!), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgname}", HttpUtility.UrlEncode(pkg.Id), StringComparison.InvariantCultureIgnoreCase)
                                .Replace("{pkgver}", HttpUtility.UrlEncode(pkg.Version), StringComparison.InvariantCultureIgnoreCase);
                        }
                    }
                    try
                    {
                        using HttpResponseMessage response = await httpClient.GetAsync(uridownload, stoppingToken);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            pkg.ResumeInfo = response.StatusCode.ToString();
                            resumePublishErr.Add(pkg);
                        }
                        else
                        {
                            var contentdld = await response.Content.ReadAsByteArrayAsync(stoppingToken);
                            using (FileStream packageStream = File.OpenWrite(pathdownload))
                            {
                                await packageStream.WriteAsync(contentdld, 0, contentdld.Length, stoppingToken);
                                await packageStream.FlushAsync(stoppingToken);
                                hascopy = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        pkg.ResumeInfo = ex.Message;
                        resumePublishErr.Add(pkg);
                    }
                }
                if (hascopy && AppConstants.IsAzuredevopsToFile)
                {
                    resumePublishOk.Add(pkg);
                    if (!newItemExport.Contains(pkg.Id))
                    {
                        newItemExport.Add(pkg.Id);
                    }
                }
                else if (hascopy && (AppConstants.IsBetweenAzuredevops || AppConstants.IsFileToAzuredevops))
                {
                    if (publishprotocol == ProtocolsTransferConstant.NameNugetProtocol)
                    {
                        string targetFeed;
                        if (target.Seleted!.project != null && target.Seleted!.project!.name! != null)
                        {
                            targetFeed = ProtocolsTransferConstant.UriNugetPackageScopedSource
                               .Replace("{baseorg}", AzureDevopsPrefix(target.Uribase, target.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                               .Replace("{projectname}", target.Seleted!.project!.name, StringComparison.InvariantCultureIgnoreCase)
                               .Replace("{feedname}", target.Seleted!.name!, StringComparison.InvariantCultureIgnoreCase);
                        }
                        else
                        {
                            targetFeed = ProtocolsTransferConstant.UriNugetPackageSource
                               .Replace("{baseorg}", AzureDevopsPrefix(target.Uribase, target.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                               .Replace("{feedname}", target.Seleted!.name!, StringComparison.InvariantCultureIgnoreCase);
                        }

                        var packageSource = new PackageSource(targetFeed)
                        {
                            Credentials = new PackageSourceCredential(
                            source: targetFeed,
                            username: IsEmptyUserCredential ? "" : target.Seleted!.name!, // not important - any value
                            passwordText: target.Passsword,
                            isPasswordClearText: true,
                            validAuthenticationTypesText: null)
                        };
                        try
                        {
                            SourceRepository repository;
                            PackageUpdateResource resource;
                            try
                            {
                                repository = Repository.Factory.GetCoreV3(packageSource);
                                resource = await repository.GetResourceAsync<PackageUpdateResource>(cancellationToken);
                                await resource.Push(
                                    new string[] { new FileInfo(pathdownload).FullName },
                                    symbolSource: null,
                                    timeoutInSecond: defaultsettings.timeoutpush,
                                    disableBuffering: false,
                                    getApiKey: packageSource =>
                                    {
                                        return "az"; //any value
                                    },
                                    getSymbolApiKey: packageSource => null,
                                    noServiceEndpoint: false,
                                    skipDuplicate: false,
                                    symbolPackageUpdateResource: null,
                                    new NuGet.Common.NullLogger());
                            }
                            catch (Exception) //bug ? try with empty value!
                            {
                                IsEmptyUserCredential = true;
                                packageSource = new PackageSource(targetFeed)
                                {
                                    Credentials = new PackageSourceCredential(
                                    source: targetFeed,
                                    username: "", // try with empty value!
                                    passwordText: target.Passsword,
                                    isPasswordClearText: true,
                                    validAuthenticationTypesText: null)
                                };
                                repository = Repository.Factory.GetCoreV3(packageSource);
                                resource = await repository.GetResourceAsync<PackageUpdateResource>(cancellationToken);
                                await resource.Push(
                                    new string[] { pathdownload },
                                    symbolSource: null,
                                    timeoutInSecond: defaultsettings.timeoutpush,
                                    disableBuffering: false,
                                    getApiKey: packageSource =>
                                    {
                                        return "az"; //any value
                                    },
                                    getSymbolApiKey: packageSource => null,
                                    noServiceEndpoint: false,
                                    skipDuplicate: false,
                                    symbolPackageUpdateResource: null,
                                    new NuGet.Common.NullLogger());
                            }
                            resumePublishOk.Add(pkg);
                            if (!newItemExport.Contains(pkg.Id))
                            {
                                newItemExport.Add(pkg.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            pkg.ResumeInfo = ex.Message;
                            resumePublishErr.Add(pkg);
                        }
                        finally
                        {
                            if (AppConstants.IsBetweenAzuredevops)
                            {
                                File.Delete(pathdownload);
                            }
                        }
                    }
                    else if (publishprotocol == ProtocolsTransferConstant.NameNpmProtocol)
                    {
                        try
                        {
                            var (path, Errorinfo) = await NpmCommands.Push(npmregistry!, new FileInfo(pathdownload).FullName, defaultsettings.timeoutpush, _logger, stoppingToken);
                            if (Errorinfo == null)
                            {
                                resumePublishOk.Add(pkg);
                                if (!newItemExport.Contains(pkg.Id))
                                {
                                    newItemExport.Add(pkg.Id);
                                }
                            }
                            else
                            {
                                pkg.ResumeInfo = Errorinfo;
                                resumePublishErr.Add(pkg);
                            }
                        }
                        finally
                        {
                            if (AppConstants.IsBetweenAzuredevops)
                            {
                                File.Delete(pathdownload);
                            }
                        }
                    }
                }
                index++;
                return new ProgressBarInfo(perc, index >= publishsource.Count, $"({index}/{publishsource.Count}) {(pkg.Id.Length <= 35 ? pkg.Id : pkg.Id[..35])}{(pkg.Id.Length > 35 ? "..." : "")}:{pkg.Version}", index);
            }

        }
    }
}
