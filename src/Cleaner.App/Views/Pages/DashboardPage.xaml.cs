using Cleaner.App.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace Cleaner.App.Views.Pages;

public partial class DashboardPage : INavigableView<CareHubViewModel>
{
    public CareHubViewModel ViewModel { get; }

    public DashboardPage(CareHubViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
