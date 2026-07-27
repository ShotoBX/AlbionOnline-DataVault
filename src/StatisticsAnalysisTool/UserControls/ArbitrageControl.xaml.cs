using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace StatisticsAnalysisTool.UserControls;

public partial class ArbitrageControl
{
    private ListSortDirection _lastSortDirection = ListSortDirection.Ascending;

    public ArbitrageControl()
    {
        InitializeComponent();
    }

    private void ResultsColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel
            || sender is not GridViewColumnHeader { Tag: string sortBy })
        {
            return;
        }

        var view = (CollectionView) CollectionViewSource.GetDefaultView(mainWindowViewModel.ArbitrageBindings.Results);
        _lastSortDirection = _lastSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortBy, _lastSortDirection));
        view.Refresh();
    }

    private void AddToWatchlist_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.ArbitrageBindings.AddToWatchlist();
    }

    private async void ArbitrageScan_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        await mainWindowViewModel.ArbitrageBindings.ScanAsync();
    }

    private async void ArbitrageScanCategory_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        await mainWindowViewModel.ArbitrageBindings.ScanCategoryAsync();
    }

    private void ArbitrageCancel_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.ArbitrageBindings.CancelScan();
    }

    private void RemoveFromWatchlist_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: Item item })
        {
            mainWindowViewModel.ArbitrageBindings.RemoveFromWatchlist(item);
        }
    }
}
