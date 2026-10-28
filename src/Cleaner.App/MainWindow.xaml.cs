using System.Windows;
using System.Windows.Media;
using Cleaner.App.Services;
using Cleaner.App.ViewModels;
using Cleaner.App.Views.Pages;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Cleaner.App.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Cleaner.App;

public partial class MainWindow : FluentWindow
{
    private readonly AppSettings _settings;
    readonly CareHubViewModel careHub;

    public MainWindow(MainWindowViewModel viewModel, AppSettings settings, IServiceProvider services,
        RunningTaskRegistry taskRegistry)
    {
        _settings = settings;
        DataContext = viewModel;
        InitializeComponent();
        careHub=services.GetRequiredService<CareHubViewModel>();SmartCareActions.DataContext=careHub;
        CareMotion.Enabled=settings.EnableAnimations;
        settings.PropertyChanged+=(_,args)=>{if(args.PropertyName==nameof(AppSettings.EnableAnimations))CareMotion.Enabled=settings.EnableAnimations;};
        Width=Math.Min(1180,SystemParameters.WorkArea.Width-40);
        Height=Math.Min(790,SystemParameters.WorkArea.Height-40);
        MinHeight=Math.Min(640,Height);MinWidth=Math.Min(980,Width);

        RootNavigation.SetServiceProvider(services);
        TasksHost.DataContext = taskRegistry;

        IsVisibleChanged+=(_,_)=>App.Monitor?.SetDashboardVisible(IsVisible);
        Loaded += async (_, _) => { RootNavigation.Navigate(typeof(DashboardPage)); await careHub.RefreshReadingsAsync(); await careHub.RefreshStorageAsync(); };
    }

    private void TasksTriggerButton_Click(object sender, RoutedEventArgs e)
    {
        TasksPopup.IsOpen = !TasksPopup.IsOpen;
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        RootNavigation.Navigate(typeof(AboutPage));
    }
    private void FocusMode_Click(object sender,RoutedEventArgs e)=>RootNavigation.Navigate(typeof(FocusPage));
    private void MiniMonitor_Click(object sender,RoutedEventArgs e)=>App.Monitor?.ShowMonitor();
    private async void SmartCare_Click(object sender,RoutedEventArgs e)
    {
        RootNavigation.Navigate(typeof(DashboardPage));
        if(!careHub.SmartCareRunning)await careHub.StartSmartCareAsync();
    }
    public void NavigateTo(Type page)
    {
        RootNavigation.Navigate(page);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplicationThemeManager.Apply(_settings.UseDarkTheme ? ApplicationTheme.Dark : ApplicationTheme.Light, WindowBackdropType.None, updateAccent: false);
        PreviewTheme.Apply(_settings.UseDarkTheme);
        _settings.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(AppSettings.UseDarkTheme)) PreviewTheme.Apply(_settings.UseDarkTheme); };
    }
}
