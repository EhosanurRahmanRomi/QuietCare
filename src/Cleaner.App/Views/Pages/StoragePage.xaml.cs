using Cleaner.App.ViewModels.Pages;
using Wpf.Ui.Controls;
namespace Cleaner.App.Views.Pages;
public partial class StoragePage : INavigableView<CareHubViewModel>
{
    public CareHubViewModel ViewModel {get;}
    public StoragePage(CareHubViewModel viewModel){ViewModel=viewModel;DataContext=this;InitializeComponent();}
}
