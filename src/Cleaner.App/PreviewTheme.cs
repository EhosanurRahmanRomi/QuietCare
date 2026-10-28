using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
namespace Cleaner.App;

internal static class PreviewTheme
{
    public static void Apply(bool dark)
    {
        ApplicationAccentColorManager.Apply(Color.FromRgb(21, 151, 122),dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        var colors=new Dictionary<string,string>
        {
            ["ApplicationBackgroundBrush"]=dark?"#10151D":"#F4F6FA",
            ["NavigationViewContentBackground"]=dark?"#121923":"#F7FAFF",
            ["CardBackgroundFillColorDefaultBrush"]=dark?"#1A222E":"#FFFFFF",
            ["CardStrokeColorDefaultBrush"]=dark?"#2D3746":"#D7E2F0",
            ["TextFillColorPrimaryBrush"]=dark?"#EDF2FA":"#15243B",
            ["TextFillColorSecondaryBrush"]=dark?"#ADBED8":"#4E6380",
            ["NavigationViewItemForeground"]=dark?"#C9D8ED":"#253C59",
            ["NavigationViewItemForegroundPointerOver"]=dark?"#FFFFFF":"#15243B",
            ["NavigationViewItemBackgroundSelected"]=dark?"#173E39":"#D7F4E9",
            ["NavigationViewItemBackgroundSelectedLeftFluent"]=dark?"#173E39":"#D7F4E9",
            ["NavigationViewItemBackgroundPointerOver"]=dark?"#1C304B":"#E2EEFA",
            ["NavigationViewItemBorderBrush"]= "#00224564",
            ["CleanerAccentBrush"]="#4EC2F4",
            ["AccentFillColorDefaultBrush"]="#11856B",
            ["AccentFillColorSecondaryBrush"]="#159779",
            ["AccentFillColorTertiaryBrush"]="#0D715C",
            ["SafeBrush"] = dark?"#61DDB1":"#137654",
            ["CareMemorySurface"]=dark?"#112F32":"#EBFAF5",
            ["CareMemoryStroke"]=dark?"#27625D":"#A3D9C9",
            ["CareMemoryAccent"]=dark?"#68EAC6":"#14765A",
            ["CareSecuritySurface"]=dark?"#172D47":"#EDF5FF",
            ["CareSecurityStroke"]=dark?"#335F88":"#BDD5F1",
            ["CareSecurityAccent"]=dark?"#85C8FF":"#216AAC",
            ["CareCleanupSurface"]=dark?"#2B2442":"#F6F0FF",
            ["CareCleanupStroke"]=dark?"#5D477F":"#DAC8EF",
            ["CareCleanupAccent"]=dark?"#C8AAFF":"#7950AC",
            ["CareDriveSurface"]=dark?"#342B28":"#FFF5E8",
            ["CareDriveStroke"]=dark?"#75543A":"#EACDA7",
            ["CareDriveAccent"]=dark?"#FFD19A":"#946022",
            ["CareAttentionBrush"]=dark?"#FFD19A":"#955416",
            ["CareFailureBrush"]=dark?"#FF9EA8":"#B42337",
            ["CareRunningBrush"]=dark?"#85C8FF":"#216AAC",
            ["CarePrimaryForeground"]="#FFFFFF",
            ["CareAccentSurface"]=dark?"#173A35":"#E4F7F0",
            ["CareAccentBrush"]=dark?"#62DFBB":"#087C61",
            ["CareSurfaceBrush"]=dark?"#19212C":"#FFFFFF",
            ["CareDividerBrush"]=dark?"#2D3746":"#E2E7EF",
            ["CareMutedBrush"]=dark?"#9BAEC8":"#66758D",
        };
        foreach(var item in colors)
        {
            var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.Value));brush.Freeze();
            Application.Current.Resources[item.Key]=brush;
        }
    }
}
