namespace PackagesTransfer
{
    internal static class AppConstants
    {
        private static string _seletedTypeTransfer = string.Empty;

        public const string BetweenAzuredevops = "Between Azure-devops";
        public const string FileToAzuredevops = "File System to Azure-devops";
        public const string AzuredevopsToFile = "Azure-devops to File System";

        public const string FileNameUserSettings = "usersettings.json";
        public const string HttpClientNameAzure = "AzureServer";

        public const string NameOriSource = "Source";
        public const string NameOriTarget = "Target";
        public const string BannerTitle = "Packages Transfer";

        public static bool IsOriSource(string value) => value == NameOriSource;
        public static bool IsOriTarget(string value) => value == NameOriTarget;


        public static string[] TypesTransfer = new string[]
        {
            BetweenAzuredevops,
            FileToAzuredevops,
            AzuredevopsToFile
        };

        public static void SelectedTypeTransfer(string value)
        {
            if (value == BetweenAzuredevops ||
                value == FileToAzuredevops ||
                value == AzuredevopsToFile)
            {
                _seletedTypeTransfer = value;
                return;
            }
            throw new ArgumentException("Ivalid value", nameof(value));
        }

        public static bool IsBetweenAzuredevops => _seletedTypeTransfer == BetweenAzuredevops;
        public static bool IsFileToAzuredevops => _seletedTypeTransfer == FileToAzuredevops;
        public static bool IsAzuredevopsToFile => _seletedTypeTransfer == AzuredevopsToFile;
    }
}
