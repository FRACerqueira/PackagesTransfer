using PPlus.Objects;
using PPlus;
using System.Text;
using PackagesTransfer.Models;
using PackagesTransfer.Protocols;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public ProcessReadPackges AzureReadPackges(string protocol, UpstreamSource[] filterupstream, HttpClient httpClient,int takequery, string baseuri, string pwdpat, string prompt, string promptdesc, CancellationToken cancellationToken)
        {
            string _CurrentArtefact = string.Empty;

            string sufixfile = string.Empty;
            if (protocol == FeedTransferConstants.NameNugetProtocol)
            {
                sufixfile = FeedTransferConstants.SufixNugetProtocol;
            }
            else if (protocol == FeedTransferConstants.NameNpmProtocol)
            {
                sufixfile = FeedTransferConstants.SufixNpmProtocol;
            }
            else 
            {
                _logger?.LogError($"Error AzureReadPackges protocol not implemented: {protocol}");
                throw new Exception($"Error AzureReadPackges protocol not implemented: {protocol}");
            }

            ResultPromptPlus<IEnumerable<ResultProcess>> process = PromptPlus.WaitProcess(prompt, promptdesc)
              .RefreshDescription(() => _CurrentArtefact)
              .AddProcess(new SingleProcess(async (StopApp) =>
              {
                  try
                  {
                      int skip = 0;
                      int pkgcounts = 0;
                      List<PackageInfo> resultpkg = new();
                      while (true)
                      {
                          try
                          {
                              httpClient.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));

                              httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                                  Convert.ToBase64String(
                                      Encoding.ASCII.GetBytes(
                                          string.Format("{0}:{1}", "", pwdpat))));

                              var aux = baseuri.Replace("{skip}", skip.ToString());
                              aux = aux.Replace("{top}", takequery.ToString());
                              PackageRoot? Details;

                              using (HttpResponseMessage responsepkg = await httpClient.GetAsync(aux, StopApp))
                              {
                                  if (responsepkg.StatusCode != HttpStatusCode.OK)
                                  {
                                      throw new Exception($"{(int)responsepkg.StatusCode}:{responsepkg.StatusCode}");
                                  }
                                  string result = await responsepkg.Content.ReadAsStringAsync(StopApp);
                                  Details = JsonSerializer.Deserialize<PackageRoot>(result);
                              }
                              if (!Details!.value.Any()) break;
                              skip += Details!.count;
                              if (filterupstream.Length > 0)
                              {
                                  _CurrentArtefact = $"Found {skip} artefacts, reading and filtering(Upstream Sources) versions...";
                              }
                              else
                              {
                                  _CurrentArtefact = $"Found {skip} artefacts, reading versions...";
                              }
                              foreach (PackageValue itempkg in Details.value)
                              {
                                  using HttpResponseMessage responsever = await httpClient.GetAsync(itempkg._links.versions.href, StopApp);
                                  if (responsever.StatusCode != HttpStatusCode.OK)
                                  {
                                      throw new Exception($"{(int)responsever.StatusCode}:{responsever.StatusCode}");
                                  }
                                  string result = await responsever.Content.ReadAsStringAsync(StopApp);
                                  PackageVersionRoot? versiondetail = JsonSerializer.Deserialize<PackageVersionRoot>(result);
                                  if (filterupstream.Length == 0)
                                  {
                                      pkgcounts++;
                                  }
                                  var found = false;
                                  foreach (PackageVersionValue item in versiondetail!.value.Where(x => !x.isDeleted))
                                  {
                                      if (filterupstream.Length > 0)
                                      {
                                          if (!item.sourceChain.Any(x => filterupstream.Any(d => d.id == x.id)))
                                          {
                                              if (!found)
                                              {
                                                  found = true;
                                                  pkgcounts++;
                                              }
                                              resultpkg.Add(new PackageInfo { Protocol = protocol, Id = itempkg.normalizedName, Version = item.normalizedVersion, FileName = $"{itempkg.normalizedName}.v{item.normalizedVersion}{sufixfile}" });
                                          }
                                      }
                                      else
                                      {
                                          resultpkg.Add(new PackageInfo { Protocol = protocol, Id = itempkg.normalizedName, Version = item.normalizedVersion, FileName = $"{itempkg.normalizedName}.v{item.normalizedVersion}{sufixfile}" });
                                      }
                                  }
                              }
                          }
                          catch
                          {
                              throw;
                          }
                      }
                      return Task.FromResult<object>(new ProcessReadPackges
                      {
                          DistinctQtd = pkgcounts,
                          Packages = resultpkg.ToArray()
                      }); 
                  }
                  catch (Exception ex)
                  {
                      _logger?.LogError($"Error on AzureReadPackges: {ex}");
                      throw;
                  }
              }, processTextResult: (x) => ""))
              .Run(cancellationToken);

            if (process.IsAborted)
            {
                ExitTanks(1);
            }

            return (ProcessReadPackges)process.Value.First().ValueProcess;

        }
    }
}
