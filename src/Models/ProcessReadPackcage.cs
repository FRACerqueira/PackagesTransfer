namespace PackagesTransfer.Models
{
    internal class ProcessReadPackges
    {
        public int DistinctQtd { get; set; }
        public PackageInfo[] Packages { get; set; } = Array.Empty<PackageInfo>();
    }
}
