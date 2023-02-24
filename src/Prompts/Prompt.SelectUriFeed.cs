using PackagesTransfer.Models;
using Microsoft.Extensions.Logging;
using PPlus;
using PPlus.Objects;
using System.ComponentModel.DataAnnotations;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public string SelectUriFeed(string keyhistory, string prompt, string promptdesc, string defaultvalue,Defaultsettings defaultsettings, CancellationToken cancellationToken)
        {
            ResultPromptPlus<string> input = PromptPlus.Readline($"{prompt}: ", promptdesc)
                .EnabledHistory(true)
                .InitialValue(defaultvalue)
                .FinisWhenHistoryEnter(true)
                .MinimumPrefixLength(5)
                .TimeoutHistory(TimeSpan.FromDays(defaultsettings.timeouthistorydays))
                .MaxHistory((byte)defaultsettings.maxhistory)
                .FileNameHistory($"FeedTransfer.{keyhistory}")
                .SaveHistoryAtFinish(true)
                .AddValidator(PromptPlusValidators.Required())
                .AddValidator(ChkUri())
                .Run(cancellationToken);

            if (input.IsAborted)
            {
                ExitTanks(1);
            }
            _logger?.LogInformation($"SelectUriFeed({defaultvalue}) - {prompt} : {input.Value}");
            return input.Value;

            static Func<object, ValidationResult> ChkUri(string? errorMessage = null)
            {
                return input =>
                {
                    return input == null
                        ? new ValidationResult(errorMessage ?? "Uri empty")
                        : input is not string
                        ? new ValidationResult(errorMessage ?? "Uri not valid")
                        : input is string strValue && string.IsNullOrEmpty(strValue)
                        ? new ValidationResult(errorMessage ?? "Uri empty")
                        : !Uri.TryCreate(input.ToString(), UriKind.Absolute, out _)
                        ? new ValidationResult(errorMessage ?? "Uri invalid")
                        : ValidationResult.Success!;
                };
            }
        }
    }
}