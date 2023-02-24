namespace PackagesTransfer.Models
{
    internal class ProcessTransfer
    {
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;
        public string TempFolder { get; set; } = null!;
        public int NewDistinctQtd { get; set; } = 0;
        public PackageInfo[] PackagesOk { get; set; } = Array.Empty<PackageInfo>();
        public PackageInfo[] PackagesErr { get; set; } = Array.Empty<PackageInfo>();
    }
}
