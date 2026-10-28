using Cleaner.App.ViewModels.Pages;
using Wpf.Ui.Controls;
namespace Cleaner.App.Views.Pages;
public partial class ProtectionPage : INavigableView<CareHubViewModel>
{
    public CareHubViewModel ViewModel {get;}
    public ProtectionPage(CareHubViewModel viewModel){ViewModel=viewModel;DataContext=this;InitializeComponent();}
}
