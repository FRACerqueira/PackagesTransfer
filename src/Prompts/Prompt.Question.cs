using PPlus;
using PPlus.Objects;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public bool Question(string prompt, string promptdesc, bool defaultvalue, CancellationToken cancellationToken)
        {
            ResultPromptPlus<bool> quest = PromptPlus.
                 SliderSwitch(prompt,promptdesc)
                 .OnValue("Yes")
                 .OffValue("No")
                 .Default(defaultvalue)
                 .Run(cancellationToken);
            if (quest.IsAborted)
            {
                ExitTanks(1);
            }
            return quest.Value;
        }
    }
}