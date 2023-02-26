namespace PackagesTransfer.Protocols;

internal class PackageRoot
{
    public int count { get; set; }
    public PackageValue[] value { get; set; } = null!;
}
