namespace PackagesTransfer.Models
{
    internal class StatusProtocols
    {
        public bool EnabledNuget { get; set; } = false;
        public bool EnabledNpm { get; set; } = false;
        public string? NugetVersion { get; set; } = string.Empty;
        public string? NpmVersion { get; set; } = string.Empty;
        public string? NpmUserFileLocation { get; set; } = string.Empty;

    }
}
