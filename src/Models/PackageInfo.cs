namespace PackagesTransfer.Models;

internal class PackageInfo
{
    public string Id { get; set; } = null!;
    public string? FileName { get; set; }
    public string Version { get; set; } = null!;
    public string? ResumeInfo { get; set; }
    public string? Protocol { get; set; }
}
