using PPlus;
using System.Runtime.InteropServices;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        public void ExitTanks(int? exitcode = null)          
        {
            PromptPlus.WriteLine();
            PromptPlus.Banner(AppConstants.BannerTitle).Run(ConsoleColor.Green);
            PromptPlus.WriteLine($"Version: {GetType().Assembly.GetName().Version}", ConsoleColor.DarkGreen);
            PromptPlus.WriteLine($"Thanks for using Packages Transfer!", ConsoleColor.DarkGreen);
            PromptPlus.WriteLine($"Check new versions on https://fracerqueira.github.io/PackagesTransfer/)", ConsoleColor.DarkGreen);
            if (exitcode.HasValue)
            {
                var kp = PromptPlus.KeyPress().Run();
                if (kp.IsAborted)
                {
                    Environment.Exit(1);
                }
                Environment.Exit(exitcode.Value);
            }
        }
    }
}
