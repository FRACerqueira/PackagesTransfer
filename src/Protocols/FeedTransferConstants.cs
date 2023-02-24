namespace PackagesTransfer.Protocols;

internal static class FeedTransferConstants
{
    public const string UriFeedList = "{baseorg}/_apis/packaging/feeds";

    public const string UriPackageList = "{baseorg}/_apis/packaging/Feeds/{feedname}/packages?$top={top}&$skip={skip}&protocolType={filtertype}";
    public const string UriPackageScopedList = "{baseorg}/{projectname}/_apis/packaging/Feeds/{feedname}/packages?$top={top}&$skip={skip}&protocolType={filtertype}";

    public const string UriNugetPackageDownload = "{baseorg}/_apis/packaging/feeds/{feedname}/nuget/packages/{pkgname}/versions/{pkgver}/content";
    public const string UriNugetPackageScopedDownload = "{baseorg}/{projectname}/_apis/packaging/feeds/{feedname}/nuget/packages/{pkgname}/versions/{pkgver}/content";

    public const string UriNugetPackageSource = "{baseorg}/_packaging/{feedname}/nuget/v3/index.json";
    public const string UriNugetPackageScopedSource = "{baseorg}/{projectname}/_packaging/{feedname}/nuget/v3/index.json";

    public const string UriNmpPackageDownload = "{baseorg}/_apis/packaging/feeds/{feedname}/npm/packages/{pkgname}/versions/{pkgver}/content";
    public const string UriNmpPackageScopedDownload = "{baseorg}/{projectname}/_apis/packaging/feeds/{feedname}/npm/packages/{pkgname}/versions/{pkgver}/content";
    public const string UriNmpPackagebasePush = "{baseorg}/_packaging/{feedname}/npm/";
    public const string UriNmpPackagebaseScopedPush = "{baseorg}/{projectname}/_packaging/{feedname}/npm/";

    public const string NameNugetProtocol = "Nuget";
    public const string NameNpmProtocol = "Npm";
    public const string NamePrefixfeeds = "feeds";
    public const string NamePrefixpkgs = "pkgs";
    public const string SufixNugetProtocol = ".nupkg";
    public const string SufixNpmProtocol = ".tgz";
    public const string NameTempFolder = "TransferTmp";

    public static string[] NamesProtocol => new string[] { NameNugetProtocol, NameNpmProtocol };

}
