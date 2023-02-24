using PPlus;
using PPlus.Objects;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public string Password(int minlength, string prompt, string promptdesc, CancellationToken cancellationToken)
        {
            ResultPromptPlus<string> input = PromptPlus
               .Input(prompt, promptdesc)
               .AddValidator(PromptPlusValidators.Required())
               .AddValidator(PromptPlusValidators.MinLength(minlength))
               .IsPassword(true)
               .Run(cancellationToken);

            if (input.IsAborted)
            {
                ExitTanks(1);
            }
            return input.Value;
        }
    }
}