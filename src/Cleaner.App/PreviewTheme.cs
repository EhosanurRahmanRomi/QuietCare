using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
namespace Cleaner.App;

internal static class PreviewTheme
{
    public static void Apply(bool dark)
    {
        ApplicationAccentColorManager.Apply(Color.FromRgb(78, 194, 244),dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        var colors=new Dictionary<string,string>
        {
            ["ApplicationBackgroundBrush"]=dark?"#0D1424":"#F2F6FC",
            ["NavigationViewContentBackground"]=dark?"#111E32":"#F7FAFF",
            ["CardBackgroundFillColorDefaultBrush"]=dark?"#1A2B44":"#FFFFFF",
            ["CardStrokeColorDefaultBrush"]=dark?"#2B4262":"#D7E2F0",
            ["TextFillColorPrimaryBrush"]=dark?"#ECF3FF":"#15243B",
            ["TextFillColorSecondaryBrush"]=dark?"#ADBED8":"#4E6380",
            ["NavigationViewItemForeground"]=dark?"#C9D8ED":"#253C59",
            ["NavigationViewItemForegroundPointerOver"]=dark?"#FFFFFF":"#15243B",
            ["NavigationViewItemBackgroundSelected"]=dark?"#224564":"#D8EEFF",
            ["NavigationViewItemBackgroundSelectedLeftFluent"]=dark?"#224564":"#D8EEFF",
            ["NavigationViewItemBackgroundPointerOver"]=dark?"#1C304B":"#E2EEFA",
            ["NavigationViewItemBorderBrush"]= "#00224564",
            ["CleanerAccentBrush"]="#4EC2F4",
            ["AccentFillColorDefaultBrush"]="#126A99",
            ["AccentFillColorSecondaryBrush"]="#167EAC",
            ["AccentFillColorTertiaryBrush"]="#155B81",
            ["SafeBrush"] = dark?"#61DDB1":"#137654",
        };
        foreach(var item in colors)
        {
            var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.Value));brush.Freeze();
            Application.Current.Resources[item.Key]=brush;
        }
    }
}
