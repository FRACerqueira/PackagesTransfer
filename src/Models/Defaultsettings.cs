using PackagesTransfer.Protocols;

namespace PackagesTransfer.Models;
internal class Defaultsettings
{
    public string prefixurifeedsource { get; set; } = FeedTransferConstants.NamePrefixfeeds;
    public string prefixurifeedtarget { get; set; } = FeedTransferConstants.NamePrefixfeeds;
    public string prefixuripkgsource { get; set; } = FeedTransferConstants.NamePrefixpkgs;
    public string prefixuripkgtarget { get; set; } = FeedTransferConstants.NamePrefixpkgs;
    public string sourceuri { get; set; } = "https://";
    public string targeturi { get; set; } = "https://";
    public int pagelength { get; set; } = 10;
    public int takequery { get; set; } = 20;
    public int timeouthistorydays { get; set; } = 30;
    public int maxhistory { get; set; } = 5;
    public string tmpfolder { get; set; } = FeedTransferConstants.NameTempFolder;
    public int timeoutcmd { get; set; } = 30000;
    public int timeoutpush { get; set; } = 90000;
    public string protocols { get; } = $"{FeedTransferConstants.NameNugetProtocol};{FeedTransferConstants.NameNpmProtocol}";
}
