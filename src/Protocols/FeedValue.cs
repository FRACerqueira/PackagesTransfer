namespace PackagesTransfer.Protocols;

internal class FeedValue
{
    public string? name { get; set; }
    public UpstreamSource[] upstreamSources { get; set; } = Array.Empty<UpstreamSource>();
    public ProjectScope? project { get; set; }
}
