using PPlus;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public void ShowTitle()
        {
            PromptPlus.Banner(AppConstants.BannerTitle)
                .Run(ConsoleColor.Green);
            PromptPlus.WriteLine($"Version: {GetType().Assembly.GetName().Version}. Check new versions on https://fracerqueira.github.io/PackagesTransfer/)", ConsoleColor.Green);
            PromptPlus.WriteLine("Transfer packages between source to target with protocols Nuget and Npm", ConsoleColor.Green);
            PromptPlus.WriteLine("Powers by:", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine("- Interactive command-line PromptPlus(https://fracerqueira.github.io/PromptPlus/)", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine("  Tips for PromptPlus Controls:", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine("    * Press F1 to show Tooltip in prompt controls", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine("    * For multi-select items, Press F8 to check/uncheck items,F5: All, F6:Invert", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine("    * All controls using GNU(https://en.wikipedia.org/wiki/GNU_Readline) Readline-Emacs keyboard shortcuts.", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine("- External command-line CliWrap(https://github.com/Tyrrrz/CliWrap)", PromptPlus.ColorSchema.Hint);
            PromptPlus.WriteLine();
        }

    }
}
