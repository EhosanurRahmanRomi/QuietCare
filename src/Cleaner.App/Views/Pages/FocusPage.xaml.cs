using Cleaner.App.ViewModels.Pages;
using Wpf.Ui.Controls;
namespace Cleaner.App.Views.Pages;
public partial class FocusPage : INavigableView<FocusViewModel>
{
    public FocusViewModel ViewModel {get;}
    public FocusPage(FocusViewModel viewModel){ViewModel=viewModel;DataContext=this;InitializeComponent();}
}
