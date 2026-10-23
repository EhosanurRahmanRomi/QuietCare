using System.Windows.Controls;
using Cleaner.App.ViewModels.Pages;
namespace Cleaner.App.Views.Pages;
public partial class CleanupHistoryPage : UserControl
{
    public RecoveryViewModel ViewModel { get; } = new();
    public CleanupHistoryPage() { InitializeComponent(); DataContext = this; Loaded += (_, _) => ViewModel.Refresh(); }
}
