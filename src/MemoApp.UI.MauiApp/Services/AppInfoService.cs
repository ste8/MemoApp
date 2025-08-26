using System.Reflection;

namespace MemoApp.UI.MauiApp.Services;

/// <summary>
/// Cross-platform service for retrieving application information
/// </summary>
public class AppInfoService : IAppInfoService
{
    private readonly Lazy<AppInfoDetails> _appDetails;

    public AppInfoService()
    {
        _appDetails = new Lazy<AppInfoDetails>(LoadAppDetails);
    }

    public string AppName => AppInfo.Name;

    public string Version => AppInfo.VersionString;

    public string BuildString => AppInfo.BuildString;

    public string DisplayVersion => $"v{Version}";

    public string PackageName => AppInfo.PackageName;

    public string Platform => DeviceInfo.Platform.ToString();

    public string FrameworkVersion => GetFrameworkVersion();

    public AppInfoDetails GetAppDetails() => _appDetails.Value;

    private AppInfoDetails LoadAppDetails()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var buildDate = GetBuildDate(assembly);
        var copyright = GetCopyright(assembly);

        return new AppInfoDetails(
            AppName: AppName,
            Version: Version,
            BuildString: BuildString,
            PackageName: PackageName,
            Platform: Platform,
            FrameworkVersion: FrameworkVersion,
            BuildDate: buildDate,
            Copyright: copyright
        );
    }

    private static string GetFrameworkVersion()
    {
        try
        {
            return Environment.Version.ToString();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static DateTime? GetBuildDate(Assembly assembly)
    {
        try
        {
            // Try to get build date from assembly attributes
            var buildDateAttribute = assembly.GetCustomAttribute<AssemblyMetadataAttribute>();
            if (buildDateAttribute?.Key == "BuildDate" && 
                DateTime.TryParse(buildDateAttribute.Value, out var buildDate))
            {
                return buildDate;
            }

            // Fallback: Use assembly file creation time (less reliable but better than nothing)
            var assemblyLocation = assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
            {
                return File.GetCreationTime(assemblyLocation);
            }
        }
        catch
        {
            // Ignore errors when trying to get build date
        }

        return null;
    }

    private static string? GetCopyright(Assembly assembly)
    {
        try
        {
            var copyrightAttribute = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            return copyrightAttribute?.Copyright;
        }
        catch
        {
            return null;
        }
    }
}