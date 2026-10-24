using CommunityToolkit.Mvvm.ComponentModel;
namespace Cleaner.App.ViewModels.Pages;
public sealed class AboutViewModel : ObservableObject
{
    public ReleaseCheckViewModel Updates { get; } = new();
    public string Version => "0.5.0-preview.2";
    public string Attribution => "Derived from PowerClean by fgilde (MIT). QuietCare monitor and safety changes by QuietCare contributors.";
}
