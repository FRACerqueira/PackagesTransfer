using PackagesTransfer.Protocols;

namespace PackagesTransfer.Models
{
    internal class FeedDataInfo
    {
        public string Prefixurifeed { get; set; } = string.Empty;
        public string Prefixuripkg { get; set; } = string.Empty;
        public string Uribase { get; set; } = string.Empty;
        public string Passsword { get; set; } = string.Empty;
        public FeedValue[] Feeds { get; set; } = Array.Empty<FeedValue>();
        public FeedValue? Seleted { get; set; }
        public UpstreamSource[] DenyUpstream { get; set; } = Array.Empty<UpstreamSource>();
        public PackageInfo[]? Packages { get; set; }
        public int DistinctPackageCount { get; set; }

    }
}
