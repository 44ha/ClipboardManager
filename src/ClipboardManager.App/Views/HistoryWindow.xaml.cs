using System.Windows;
using System.Windows.Input;
using ClipboardManager.App.ViewModels;

namespace ClipboardManager.App.Views;

/// <summary>
/// Main desktop window for Clipboard Manager.
/// </summary>
public partial class HistoryWindow : Window
{
    private HistoryViewModel ViewModel => (HistoryViewModel)DataContext;

    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += (_, _) =>
        {
            FocusSearchBox();
        };

        Activated += (_, _) =>
        {
            FocusSearchBox();
        };
    }

    public void FocusSearchBox()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OnClearSearchClick(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        FocusSearchBox();
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = string.Empty;
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (ViewModel.SelectedItem != null)
            {
                _ = ViewModel.SelectAndPasteAsync(ViewModel.SelectedItem);
                e.Handled = true;
            }
        }
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (HistoryListBox.Items.Count > 0)
            {
                HistoryListBox.Focus();
                if (HistoryListBox.SelectedItem == null)
                {
                    HistoryListBox.SelectedIndex = 0;
                }
                e.Handled = true;
            }
        }
    }

    private void OnListBoxDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedItem != null)
        {
            _ = ViewModel.SelectAndPasteAsync(ViewModel.SelectedItem);
            e.Handled = true;
        }
    }
}
