using PackagesTransfer.Models;
using Microsoft.Extensions.Logging;
using PPlus;
using PPlus.Objects;
using System.ComponentModel.DataAnnotations;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public string SelectUriPrefix(string keyhistory, string prompt, string promptdesc, string defaultvalue,Defaultsettings defaultsettings, CancellationToken cancellationToken)
        {
            ResultPromptPlus<string> input = PromptPlus.Readline($"{prompt}: ", promptdesc)
                .EnabledHistory(true)
                .InitialValue(defaultvalue)
                .FinisWhenHistoryEnter(true)
                .TimeoutHistory(TimeSpan.FromDays(defaultsettings.timeouthistorydays))
                .MaxHistory((byte)defaultsettings.maxhistory)
                .FileNameHistory($"FeedTransfer.{keyhistory}")
                .SaveHistoryAtFinish(true)
                .AddValidator(ValidateUriprefix())
                .Run(cancellationToken);


            if (input.IsAborted)
            {
                ExitTanks(1);
            }
            _logger?.LogInformation($"SelectUriPrefix({defaultvalue}) - {prompt} : {input.Value}");
            return input.Value;

            static Func<object, ValidationResult> ValidateUriprefix()
            {
                return input =>
                {
                    if (string.IsNullOrEmpty(input.ToString()))
                    {
                        return ValidationResult.Success!;
                    }
                    try
                    {
                        Uri.TryCreate("https://teste.com/", UriKind.Absolute, out Uri? uriinput);
                        UriBuilder result = new(uriinput!.Scheme, $"{input}.{uriinput.Authority}", uriinput.Port, uriinput.PathAndQuery);
                        _ = result.Uri.AbsolutePath;
                        return ValidationResult.Success!;
                    }
                    catch
                    {
                        return new ValidationResult("Invalid Uri-prefix");
                    }
                };
            }
        }
    }
}