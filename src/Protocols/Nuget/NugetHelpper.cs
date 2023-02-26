using PackagesTransfer.Models;
using System.IO.Compression;
using System.Text;

namespace PackagesTransfer.Protocols.Nuget
{
    internal static class NugetHelpper
    {
        public static PackageInfo? ExtractInfoFromfile(string pathfile)
        {
            using (var zip = ZipFile.OpenRead(pathfile))
            {
                var spec = zip.Entries.First(x => x.FullName.EndsWith(".nuspec"));
                using (var zipfile = spec.Open())
                {
                    using (StreamReader reader = new StreamReader(zipfile, Encoding.UTF8))
                    {
                        var result = reader.ReadToEnd();
                        var initag = result.IndexOf("<id>", StringComparison.InvariantCultureIgnoreCase);
                        var endtag = result.IndexOf("</id>", StringComparison.InvariantCultureIgnoreCase);
                        if (initag != -1 && endtag != -1)
                        {
                            var id = result.Substring(initag + 4, endtag - initag - 4);
                            initag = result.IndexOf("<version>", StringComparison.InvariantCultureIgnoreCase);
                            endtag = result.IndexOf("</version>", StringComparison.InvariantCultureIgnoreCase);
                            if (initag != -1 && endtag != -1)
                            {
                                var verid = result.Substring(initag + 9, endtag - initag - 9);
                                return new PackageInfo
                                {
                                    Id = id,
                                    Version = verid,
                                    FileName = new FileInfo(pathfile).Name,
                                    Protocol = ProtocolsTransferConstant.NameNugetProtocol
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
