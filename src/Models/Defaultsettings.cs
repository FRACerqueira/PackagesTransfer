using PackagesTransfer.Protocols;

namespace PackagesTransfer.Models;
internal class Defaultsettings
{
    public bool showtips { get; set; } = false;
    public string prefixurifeedsource { get; set; } = ProtocolsTransferConstant.ExamplePrefixfeeds;
    public string prefixurifeedtarget { get; set; } = ProtocolsTransferConstant.ExamplePrefixfeeds;
    public string prefixuripkgsource { get; set; } = ProtocolsTransferConstant.ExamplePrefixpkgs;
    public string prefixuripkgtarget { get; set; } = ProtocolsTransferConstant.ExamplePrefixpkgs;
    public string sourceuri { get; set; } = "https://";
    public string targeturi { get; set; } = "https://";
    public int pagelength { get; set; } = 10;
    public int takequery { get; set; } = 50;
    public int timeouthistorydays { get; set; } = 30;
    public int maxhistory { get; set; } = 5;
    public string tmpfolder { get; set; } = ProtocolsTransferConstant.NameTempFolder;
    public int timeoutcmd { get; set; } = 30000;
    public int timeoutpush { get; set; } = 90000;
    public string sufixnuget { get; set; } = ".nupkg;";
    public string sufixnpm { get; set; } = ".tgz;.jar;";
    public string protocols { get; } = $"{ProtocolsTransferConstant.NameNugetProtocol};{ProtocolsTransferConstant.NameNpmProtocol}";
}
