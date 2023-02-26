namespace PackagesTransfer.Protocols;

internal class PackageVersionRoot
{
    public int count { get; set; }
    public PackageVersionValue[] value { get; set; } = null!;
}
