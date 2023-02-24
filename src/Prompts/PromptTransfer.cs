using Microsoft.Extensions.Logging;

namespace PackagesTransfer.Prompts
{
    internal partial class PromptTransfer
    {
        private readonly ILogger<PromptTransfer> _logger;
        public PromptTransfer(ILogger<PromptTransfer> logger)
        {
            _logger = logger;
        }
    }
}
