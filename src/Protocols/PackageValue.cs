namespace PackagesTransfer.Protocols;

internal class PackageValue
{
    public string id { get; set; } = null!;
    public string normalizedName { get; set; } = null!;
    public Links _links { get; set; } = null!;
}
