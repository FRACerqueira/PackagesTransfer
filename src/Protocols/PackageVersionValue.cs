namespace PackagesTransfer.Protocols;

internal class PackageVersionValue
{
    public string id { get; set; } = null!;
    public string normalizedVersion { get; set; } = null!;
    public bool isDeleted { get; set; }
    public UpstreamSource[] sourceChain { get; set; } = Array.Empty<UpstreamSource>();
}
