using StatisticsAnalysisTool.Crafting;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace StatisticsAnalysisTool.UserControls;

public partial class CraftingControl
{
    private bool _wasPriceTextBoxFocusedOnMouseDown;

    public CraftingControl()
    {
        InitializeComponent();
    }

    private void SavedCraftingRow_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListViewItem { DataContext: SavedCrafting savedCrafting })
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.LoadSelectedCommand.Execute(savedCrafting);
    }

    private void ListBoxItemSearch_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingItemSearchResult searchResult)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectItemSearchResult(searchResult);
        listBox.SelectedItem = null;
    }

    private void CraftingItemSearch_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        OpenItemSearch();
    }

    private void CraftingItemSearch_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        OpenItemSearch();
    }

    private void OpenItemSearch()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenItemSearch();
    }

    private void CraftingItemSearch_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var newFocus = e.NewFocus as DependencyObject;

        if (IsElementOrChildOf(newFocus, CraftingItemSearchTextBox)
            || IsElementOrChildOf(newFocus, CraftingItemSearchListBox))
        {
            return;
        }

        CloseItemSearch();
    }

    private void ListBoxCraftingLocation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingLocationOption location)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectCraftingLocation(location);
        listBox.SelectedItem = null;
    }

    private void ListBoxSellPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption sellPriceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectSellPriceOption(sellPriceOption);
        listBox.SelectedItem = null;
    }

    private void ListBoxResourcePrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.Tag is not CraftingResourceEntry resource)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectResourcePriceOption(resource, priceOption);
        listBox.SelectedItem = null;
    }

    private void ListBoxJournalEmptyPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectJournalEmptyPriceOption(priceOption);
        listBox.SelectedItem = null;
    }

    private void ListBoxJournalFullPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.SelectJournalFullPriceOption(priceOption);
        listBox.SelectedItem = null;
    }

    private void CopyTextBlockTextToClipboard_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { Text: { } textBlockText }
            || string.IsNullOrWhiteSpace(textBlockText))
        {
            return;
        }

        Clipboard.SetText(textBlockText);
        e.Handled = true;
    }

    private void CraftingLocationSearch_OnGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenCraftingLocationSearch();
    }

    private void OutputUnitPrice_OnGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenSellPriceOptions();
    }

    private void OutputUnitPrice_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var newFocus = e.NewFocus as DependencyObject;

        if (IsElementOrChildOf(newFocus, OutputUnitPriceTextBox)
            || IsElementOrChildOf(newFocus, SellPriceOptionsListBox))
        {
            return;
        }

        CloseSellPriceOptions();
    }

    private void OutputUnitPrice_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenSellPriceOptions();
    }

    private void ResourcePrice_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        OpenResourcePriceOptions(sender);
    }

    private void ResourcePrice_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        OpenResourcePriceOptions(sender);
    }

    private void JournalEmptyPrice_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalEmptyPriceOptions();
    }

    private void JournalEmptyPrice_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalEmptyPriceOptions();
    }

    private void JournalFullPrice_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalFullPriceOptions();
    }

    private void JournalFullPrice_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenJournalFullPriceOptions();
    }

    private void PriceTextBox_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _wasPriceTextBoxFocusedOnMouseDown = sender is FrameworkElement frameworkElement
                                            && frameworkElement.IsKeyboardFocusWithin;
    }

    private void CraftingControl_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;

        if (!IsElementOrChildOf(source, CraftingItemSearchTextBox)
            && !IsItemSearchPopupClick(source))
        {
            CloseItemSearch();
        }

        if (!IsElementOrChildOf(source, OutputUnitPriceTextBox)
            && !IsPriceOptionPopupClick(source))
        {
            CloseSellPriceOptions();
        }

        if (!IsElementOrChildOf(source, RefiningItemSearchTextBox)
            && !IsItemSearchPopupClick(source))
        {
            mainWindowViewModel.RefiningBindings.CloseItemSearch();
        }

        if (!IsElementOrChildOf(source, RefiningOutputUnitPriceTextBox)
            && !IsPriceOptionPopupClick(source))
        {
            mainWindowViewModel.RefiningBindings.CloseAllPriceOptionPopups();
        }
    }

    private void FilterReset_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.BlackMarket?.ResetFilters();
    }

    private async void OptimizerScan_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        await mainWindowViewModel.CraftingBindings.Optimizer.ScanAsync();
    }

    private void OptimizerCancel_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.Optimizer.CancelScan();
    }

    private ListSortDirection _optimizerLastSortDirection = ListSortDirection.Ascending;

    private void OptimizerResultsColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel
            || sender is not GridViewColumnHeader { Tag: string sortBy })
        {
            return;
        }

        var view = (CollectionView) CollectionViewSource.GetDefaultView(mainWindowViewModel.CraftingBindings.Optimizer.Results);
        _optimizerLastSortDirection = _optimizerLastSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortBy, _optimizerLastSortDirection));
        view.Refresh();
    }

    private void CraftingItemFilterReset_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.ResetItemFilters();
    }

    #region Refining

    private void ListBoxRefiningItemSearch_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingItemSearchResult searchResult)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.SelectItemSearchResult(searchResult);
        listBox.SelectedItem = null;
    }

    private void RefiningItemSearch_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        OpenRefiningItemSearch();
    }

    private void RefiningItemSearch_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        OpenRefiningItemSearch();
    }

    private void OpenRefiningItemSearch()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.OpenItemSearch();
    }

    private void RefiningItemSearch_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var newFocus = e.NewFocus as DependencyObject;

        if (IsElementOrChildOf(newFocus, RefiningItemSearchTextBox)
            || IsElementOrChildOf(newFocus, RefiningItemSearchListBox))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.CloseItemSearch();
    }

    private void RefiningItemFilterReset_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.ResetItemFilters();
    }

    private void RefiningLocationSearch_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.OpenRefiningLocationSearch();
    }

    private void ListBoxRefiningLocation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingLocationOption location)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.SelectRefiningLocation(location);
        listBox.SelectedItem = null;
    }

    private void RefiningOutputUnitPrice_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.OpenSellPriceOptions();
    }

    private void RefiningOutputUnitPrice_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.OpenSellPriceOptions();
    }

    private void ListBoxRefiningSellPrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption sellPriceOption)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.SelectSellPriceOption(sellPriceOption);
        listBox.SelectedItem = null;
    }

    private void RefiningResourcePrice_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        OpenRefiningResourcePriceOptions(sender);
    }

    private void RefiningResourcePrice_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!ShouldOpenPricePopupOnMouseLeftButtonUp(sender))
        {
            return;
        }

        OpenRefiningResourcePriceOptions(sender);
    }

    private void OpenRefiningResourcePriceOptions(object sender)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: CraftingResourceEntry resource })
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.OpenResourcePriceOptions(resource);
    }

    private void ListBoxRefiningResourcePrice_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not ListBox listBox)
        {
            return;
        }

        if (listBox.Tag is not CraftingResourceEntry resource)
        {
            return;
        }

        if (listBox.SelectedItem is not CraftingSellPriceOption priceOption)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.SelectResourcePriceOption(resource, priceOption);
        listBox.SelectedItem = null;
    }

    private async void RefiningOptimizerScan_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        await mainWindowViewModel.RefiningBindings.Optimizer.ScanAsync();
    }

    private void RefiningOptimizerCancel_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RefiningBindings.Optimizer.CancelScan();
    }

    private ListSortDirection _refiningOptimizerLastSortDirection = ListSortDirection.Ascending;

    private void RefiningOptimizerResultsColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel
            || sender is not GridViewColumnHeader { Tag: string sortBy })
        {
            return;
        }

        var view = (CollectionView) CollectionViewSource.GetDefaultView(mainWindowViewModel.RefiningBindings.Optimizer.Results);
        _refiningOptimizerLastSortDirection = _refiningOptimizerLastSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortBy, _refiningOptimizerLastSortDirection));
        view.Refresh();
    }

    #endregion

    private void OpenResourcePriceOptions(object sender)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is not FrameworkElement frameworkElement)
        {
            return;
        }

        if (frameworkElement.DataContext is not CraftingResourceEntry resource)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.OpenResourcePriceOptions(resource);
    }

    private bool ShouldOpenPricePopupOnMouseLeftButtonUp(object sender)
    {
        var shouldOpen = sender is FrameworkElement frameworkElement
                         && frameworkElement.IsKeyboardFocusWithin
                         && _wasPriceTextBoxFocusedOnMouseDown;

        _wasPriceTextBoxFocusedOnMouseDown = false;

        return shouldOpen;
    }

    private void CloseSellPriceOptions()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.CloseAllPriceOptionPopups();
    }

    private void CloseItemSearch()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.CraftingBindings.CloseItemSearch();
    }

    private static bool IsElementOrChildOf(DependencyObject source, DependencyObject parent)
    {
        while (source != null)
        {
            if (ReferenceEquals(source, parent))
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static bool IsItemSearchPopupClick(DependencyObject source)
    {
        while (source != null)
        {
            if (source is FrameworkElement { DataContext: CraftingItemSearchResult })
            {
                return true;
            }

            if (source is ListBox listBox
                && listBox.Items.Count > 0
                && listBox.Items[0] is CraftingItemSearchResult)
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static bool IsPriceOptionPopupClick(DependencyObject source)
    {
        while (source != null)
        {
            if (source is FrameworkElement { DataContext: CraftingSellPriceOption })
            {
                return true;
            }

            if (source is ListBox listBox
                && listBox.Items.Count > 0
                && listBox.Items[0] is CraftingSellPriceOption)
            {
                return true;
            }

            source = GetParent(source);
        }

        return false;
    }

    private static DependencyObject GetParent(DependencyObject source)
    {
        if (source is Visual
            || source is System.Windows.Media.Media3D.Visual3D)
        {
            return VisualTreeHelper.GetParent(source);
        }

        return LogicalTreeHelper.GetParent(source);
    }

    private void PopupListBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListBox listBox || FindDescendantScrollViewer(listBox) is not { } scrollViewer)
        {
            return;
        }

        var scrollSteps = System.Math.Max(1, System.Math.Abs(e.Delta) / Mouse.MouseWheelDeltaForOneLine);

        for (var i = 0; i < scrollSteps; i++)
        {
            if (e.Delta > 0)
            {
                scrollViewer.LineUp();
            }
            else
            {
                scrollViewer.LineDown();
            }
        }

        e.Handled = true;
    }

    private static ScrollViewer FindDescendantScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            if (FindDescendantScrollViewer(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
