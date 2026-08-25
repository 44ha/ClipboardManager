using System.Windows;
using System.Windows.Input;
using ClipboardManager.App.ViewModels;

namespace ClipboardManager.App.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml.
/// </summary>
public partial class SettingsWindow : Window
{
    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Activated += (_, _) =>
        {
            ViewModel.LoadCurrentSettings();
        };
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }
}
