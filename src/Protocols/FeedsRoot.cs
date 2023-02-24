namespace PackagesTransfer.Protocols;

internal class FeedsRoot
{
    public int count { get; set; }
    public FeedValue[] value { get; set; } = Array.Empty<FeedValue>();
    public Exception? Exception { get; set; }
}
