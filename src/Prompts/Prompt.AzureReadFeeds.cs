using PackagesTransfer.Protocols;
using PPlus.Objects;
using PPlus;
using System.Text;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public FeedsRoot ReadFeeds(HttpClient httpClient, string baseuri, string pwdpat, string prompt, string promptdesc,CancellationToken cancellationToken)
        {
            ResultPromptPlus<IEnumerable<ResultProcess>> process = PromptPlus.WaitProcess(prompt, promptdesc)
              .AddProcess(new SingleProcess(async (StopApp) =>
              {
                  httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                  httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                      Convert.ToBase64String(
                          Encoding.ASCII.GetBytes(
                              string.Format("{0}:{1}", "", pwdpat))));
                  string aux = baseuri;
                  if (aux.EndsWith("/"))
                  {
                      aux = aux[..^1];
                  }
                  string feedlisturl = ProtocolsTransferConstant.UriFeedList.Replace("{baseorg}", aux, StringComparison.InvariantCultureIgnoreCase);

                  try
                  {
                      using HttpResponseMessage response = await httpClient.GetAsync(feedlisturl, StopApp);
                      if (response.StatusCode != HttpStatusCode.OK)
                      {
                          return Task.FromResult<object>(new FeedsRoot() { Exception = new HttpRequestException($"{(int)response.StatusCode}:{response.StatusCode}") });
                      }
                      string result = await response.Content.ReadAsStringAsync(StopApp);
                      FeedsRoot? Details = JsonSerializer.Deserialize<FeedsRoot>(result);
                      return Task.FromResult<object>(Details!);
                  }
                  catch (Exception ex)
                  {
                      return Task.FromResult<object>(new FeedsRoot() { Exception = ex });
                  }
              }, processTextResult: (x) => ""))
              .Run(cancellationToken);

            if (process.IsAborted)
            {
                ExitTanks(1);
            }

            ResultProcess aux = process.Value.First();

            return (FeedsRoot)aux.ValueProcess;
        }
    }
}
