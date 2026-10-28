using System.Reflection;
namespace Cleaner.App;

internal static class AppVersion
{
    public static string Current {get;}=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ??typeof(App).Assembly.GetName().Version?.ToString()??"Unknown";
}
