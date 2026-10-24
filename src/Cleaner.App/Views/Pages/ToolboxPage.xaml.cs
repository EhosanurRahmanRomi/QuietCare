using Cleaner.App.ViewModels.Pages;
using Wpf.Ui.Controls;
namespace Cleaner.App.Views.Pages;
public partial class ToolboxPage : INavigableView<CareHubViewModel>
{
    public CareHubViewModel ViewModel {get;}
    public ToolboxPage(CareHubViewModel viewModel){ViewModel=viewModel;DataContext=this;InitializeComponent();}
}
