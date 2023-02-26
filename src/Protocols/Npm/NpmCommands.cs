using CliWrap;
using Microsoft.Extensions.Logging;
using PackagesTransfer.Models;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace PackagesTransfer.Protocols.Npm
{
    internal static class NpmCommands
    {
        public static PackageInfo? ExtractInfoFromfile(string pathfile, ILogger? logger)
        {
            try
            {
                using var gzip = new GZipStream(File.OpenRead(pathfile), CompressionMode.Decompress);
                using var unzippedStream = new MemoryStream();
                gzip.CopyTo(unzippedStream);
                unzippedStream.Seek(0, SeekOrigin.Begin);
                using var reader = new TarReader(unzippedStream);
                while (reader.GetNextEntry() is TarEntry entry)
                {
                    if (entry.Name == "package/package.json")
                    {
                        using var unzippedStreamfile = new MemoryStream();
                        entry.DataStream!.CopyTo(unzippedStreamfile);
                        var aux = Encoding.UTF8.GetString(unzippedStreamfile.ToArray(), 0, (int)unzippedStreamfile.Length);
                        var info = JsonSerializer.Deserialize<NmpPackageinfo>(aux);
                        if (!string.IsNullOrEmpty(info!.name) && !string.IsNullOrEmpty(info!.version))
                        {
                            return new PackageInfo
                            {
                                Id = info.name,
                                Version = info.version,
                                FileName = new FileInfo(pathfile).Name,
                                Protocol = ProtocolsTransferConstant.NameNpmProtocol
                            };
                        }
                        break;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.ToString());
                throw;
            }
        }

        public static async Task<(NpmVersion? Version, string? ErrorInfo)> Version(int timeout,ILogger logger,  CancellationToken cancellationToken)
        {
            using var cts = new CancellationTokenSource();
            
            // When the cancellation token is triggered,
            // schedule forceful cancellation as fallback.
            using var link = cancellationToken.Register(() =>
                cts.CancelAfter(TimeSpan.FromMilliseconds(timeout))
            );


            cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();

            (NpmVersion? Version, string? ErrorInfo) result = new(null, null);
            try
            {
                var command = await Cli.Wrap("npm")
                    .WithWorkingDirectory(AppDomain.CurrentDomain.BaseDirectory)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithArguments(a => a
                    .Add("version")
                    .Add("-json"))
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(cts.Token, cancellationToken);

                if (command.ExitCode == 0)
                {
                    result.Version = JsonSerializer.Deserialize<NpmVersion>(stdOutBuffer.ToString());
                }
                else
                {
                    var err = stdErrBuffer.ToString();
                    if (string.IsNullOrEmpty(err))
                    {
                        err = "Version npm error!";
                    }
                    logger.LogError(err);
                    result.ErrorInfo = "Version npm error!";
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorInfo = "Timeout version command";
                logger.LogError(result.ErrorInfo);
            }
            catch (Exception ex)
            {
                result.ErrorInfo = "Version npm error!";
                logger.LogError(ex.ToString());
            }
            return result;
        }

        public static async Task<(string? path, string? Errorinfo)> UserConfigPath(int timeout,ILogger? logger, CancellationToken cancellationToken)
        {
            using var cts = new CancellationTokenSource();

            // When the cancellation token is triggered,
            // schedule forceful cancellation as fallback.
            using var link = cancellationToken.Register(() =>
                cts.CancelAfter(TimeSpan.FromMilliseconds(timeout))
            );

            (string? path, string? Errorinfo) result = new(null, null);
            try
            {
                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();
                var command = await Cli.Wrap("npm")
                    .WithWorkingDirectory(AppDomain.CurrentDomain.BaseDirectory)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithArguments(a => a
                    .Add("config")
                    .Add("get")
                    .Add("userconfig"))
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteAsync(cts.Token, cancellationToken);

                if (command.ExitCode == 0)
                {
                    result.path = stdOutBuffer.ToString().Replace("\n", "").Trim();
                }
                else
                {
                    var err = stdErrBuffer.ToString();
                    if (string.IsNullOrEmpty(err))
                    {
                        err = "Userconfig npm error";
                    }
                    result.Errorinfo = "Userconfig npm error!. View log to details";
                }
            }
            catch (OperationCanceledException)
            {
                result.Errorinfo = "Timeout/Canceled userconfig command";
                logger?.LogError(result.Errorinfo);
            }
            catch (Exception ex)
            {
                result.Errorinfo = "npm userconfig error";
                logger?.LogError(ex.ToString());
            }
            return result;
        }

        public static async Task<(string? path, string? Errorinfo)> Push(string register, string filename, int timeout,ILogger? logger, CancellationToken cancellationToken)
        {
            using var cts = new CancellationTokenSource();

            // When the cancellation token is triggered,
            // schedule forceful cancellation as fallback.
            using var link = cancellationToken.Register(() =>
                cts.CancelAfter(TimeSpan.FromMilliseconds(timeout))
            );

            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            (string? path, string? Errorinfo) result = new (null,null);
            try
            {
                var command = await Cli.Wrap("npm")
                    .WithWorkingDirectory(AppDomain.CurrentDomain.BaseDirectory)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithValidation(CommandResultValidation.None)
                    .WithArguments(a => a
                        .Add("publish")
                        .Add($"--registry={register}")
                        .Add(filename))
                    .ExecuteAsync(cts.Token,cancellationToken);
                if (command.ExitCode == 0)
                {
                    result.path = stdOutBuffer.ToString().Replace("\n","");
                }
                else
                {
                    var err = stdErrBuffer.ToString();
                    if (string.IsNullOrEmpty(err))
                    {
                        err = "npm publish error";
                    }
                    logger?.LogError(err);
                    result.Errorinfo = "Published error!. View log to details";
                }
            }
            catch (OperationCanceledException)
            {
                result.Errorinfo = "Timeout/Canceled Push command";
                logger?.LogError(result.Errorinfo);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex.ToString());
                result.Errorinfo = "Published error!. View log to details";
            }
            return result;
        }

        public static async Task<(string? UriRegistry, string? Error)> SetUserConfigTransfer(FeedDataInfo feedDataInfo, int timeout, ILogger? logger, CancellationToken cancellationToken)
        {
            var userpathfile = (await UserConfigPath(timeout, logger, cancellationToken)).path;
            try
            {
                string targetFeedcfg;
                string uriregistry;
                if (feedDataInfo.Seleted!.project != null && feedDataInfo.Seleted!.project!.name! != null)
                {
                    targetFeedcfg = ProtocolsTransferConstant.UriNmpPackagebaseScopedPush
                       .Replace("{baseorg}", AzureDevopsPrefixWidthoutProtocol(feedDataInfo.Uribase, feedDataInfo.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                       .Replace("{projectname}", feedDataInfo.Seleted!.project!.name, StringComparison.InvariantCultureIgnoreCase)
                       .Replace("{feedname}", feedDataInfo.Seleted!.name!, StringComparison.InvariantCultureIgnoreCase);
                    uriregistry = ProtocolsTransferConstant.UriNmpPackagebaseScopedPush
                     .Replace("{baseorg}", AzureDevopsPrefix(feedDataInfo.Uribase, feedDataInfo.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                     .Replace("{projectname}", feedDataInfo.Seleted!.project!.name, StringComparison.InvariantCultureIgnoreCase)
                     .Replace("{feedname}", feedDataInfo.Seleted!.name!, StringComparison.InvariantCultureIgnoreCase);
                }
                else
                {
                    targetFeedcfg = ProtocolsTransferConstant.UriNmpPackagebasePush
                       .Replace("{baseorg}", AzureDevopsPrefixWidthoutProtocol(feedDataInfo.Uribase, feedDataInfo.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                       .Replace("{feedname}", feedDataInfo.Seleted!.name!, StringComparison.InvariantCultureIgnoreCase);
                    uriregistry = ProtocolsTransferConstant.UriNmpPackagebasePush
                       .Replace("{baseorg}", AzureDevopsPrefix(feedDataInfo.Uribase, feedDataInfo.Prefixuripkg), StringComparison.InvariantCultureIgnoreCase)
                       .Replace("{feedname}", feedDataInfo.Seleted!.name!, StringComparison.InvariantCultureIgnoreCase);
                }
                uriregistry += "registry/";

                if (!File.Exists(userpathfile))
                {
                    File.Create(userpathfile!);
                }
                var auxlines = await File.ReadAllLinesAsync(userpathfile!,cancellationToken);
                var lines = new List<string>();
                var skip = false;
                foreach (var auxline in auxlines)
                {
                    if (auxline == "; begin auth token package-transfer")
                    {
                        skip = true;
                    }
                    if (auxline == "; end auth token package-transfer")
                    {
                        skip = false;
                    }
                    else
                    {
                        if (!skip)
                        {
                            lines.Add(auxline);
                        }
                    }
                }
                var extraline = false;
                if (lines.Count == 0)
                {
                    extraline = true;
                }
                var pwd = Convert.ToBase64String(Encoding.UTF8.GetBytes(feedDataInfo.Passsword));
                lines.Add("; begin auth token package-transfer");
                if (extraline)
                {
                    lines.Add($"strict-ssl=false");
                    lines.Add($"fetch-retry-mintimeout=20000");
                    lines.Add($"fetch-retry-maxtimeout=120000");
                }
                lines.Add($"//{targetFeedcfg}registry/:username={feedDataInfo.Seleted!.name}");
                lines.Add($"//{targetFeedcfg}registry/:_password=\"{pwd}\"");
                lines.Add($"//{targetFeedcfg}registry/:email=any@any.com");
                lines.Add($"//{targetFeedcfg}:username={feedDataInfo.Seleted!.name}");
                lines.Add($"//{targetFeedcfg}:_password=\"{pwd}\"");
                lines.Add($"//{targetFeedcfg}:email=any@any.com");
                lines.Add("; end auth token package-transfer");
                await File.WriteAllLinesAsync(userpathfile!, lines, cancellationToken);
                logger?.LogInformation($"Write OK ({userpathfile}) UserConfig nmp");
                return (uriregistry, null);
            }
            catch (Exception ex)
            {
                logger?.LogInformation($"UserConfig Error: {ex}");
                return (null, $"Set UserConfig Error!: {ex}");
            }
        }

        public static async Task<(string? path, string? Error)> ResetUserConfig(int timeout, ILogger? logger, CancellationToken cancellationToken)
        {
            var userpathfile = (await UserConfigPath(timeout,logger,cancellationToken)).path;
            try
            {
                var auxlines = await File.ReadAllLinesAsync(userpathfile!, cancellationToken);
                var lines = new List<string>();
                var skip = false;
                foreach (var auxline in auxlines)
                {
                    if (auxline == "; begin auth token package-transfer")
                    {
                        skip = true;
                    }
                    if (auxline == "; end auth token package-transfer")
                    {
                        skip = false;
                    }
                    else
                    {
                        if (!skip)
                        {
                            lines.Add(auxline);
                        }
                    }
                }
                await File.WriteAllLinesAsync(userpathfile!, lines, cancellationToken);
                logger?.LogInformation($"Write reset OK ({userpathfile}) UserConfig nmp");
                return (userpathfile!, null);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Write reset Error ({userpathfile}) UserConfig nmp: {ex}");
                return (null,$"ResetUserConfig error: {ex.Message}");
            }
        }

        private static string AzureDevopsPrefix(string value, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                if (value.EndsWith("/"))
                {
                    return value.Substring(0, value.Length - 1);
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

        private static string AzureDevopsPrefixWidthoutProtocol(string value, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                Uri.TryCreate(value, UriKind.Absolute, out Uri? uriinputaux);
                if (uriinputaux!.AbsolutePath.EndsWith("/"))
                {
                    return $"{uriinputaux!.Authority}{uriinputaux.AbsolutePath.Substring(0, uriinputaux.AbsolutePath.Length - 1)}";
                }
                return $"{uriinputaux!.Authority}{uriinputaux.AbsolutePath}";
            }
            Uri.TryCreate(value, UriKind.Absolute, out Uri? uriinput);
            UriBuilder result = new(uriinput!.Scheme, $"{prefix}.{uriinput.Authority}", uriinput.Port, uriinput.PathAndQuery);
            if (result.Uri!.AbsolutePath.EndsWith("/"))
            {
                return $"{result.Uri!.Authority}/{result.Uri.AbsolutePath.Substring(0, result.Uri.AbsolutePath.Length - 1)}";
            }
            return $"{result.Uri.Authority}/{result.Uri.AbsolutePath}";
        }


    }
}
