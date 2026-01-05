using Avalonia.Controls;
using Avalonia.Interactivity;
using CarBalanceTool.ViewModels;

namespace CarBalanceTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var storageProvider = StorageProvider;
            await vm.SelectFolderAsync(storageProvider);
        }
    }
}
