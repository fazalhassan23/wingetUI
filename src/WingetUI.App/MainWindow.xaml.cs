using System.Windows;
using WingetUI.Core;
using WingetUI.WinGet;

namespace WingetUI.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel model = new(new WinGetManager());

    public MainWindow()
    {
        InitializeComponent();
        DataContext = model;
        Loaded += async (_, _) =>
        {
            model.LoadHistory();
            await model.RefreshCommand.ExecuteAsync(null);
        };
        Closing += (_, args) =>
        {
            if (model.Busy)
                args.Cancel = MessageBox.Show(this, "An operation is active. Closing may leave its outcome unknown. Close now?",
                    "WingetUI", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes;
        };
    }

    private void SelectAllUpdates(object sender, RoutedEventArgs e) => UpdatesGrid.SelectAll();

    private void ClearUpdates(object sender, RoutedEventArgs e) => UpdatesGrid.UnselectAll();

    private void UpdateSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        int count = UpdatesGrid.SelectedItems.Count;
        UpgradeButton.Content = $"_Upgrade selected ({count})";
        UpgradeButton.IsEnabled = count > 0;
    }

    private async void UpgradeSelected(object sender, RoutedEventArgs e) =>
        await ConfirmAsync(UpdatesGrid.SelectedItems.Cast<Package>().ToArray(), upgrade: true);

    private async void InstallSelected(object sender, RoutedEventArgs e)
    {
        if (SearchGrid.SelectedItem is Package package)
            await ConfirmAsync([package], package.CanUpgrade);
    }

    private async Task ConfirmAsync(Package[] packages, bool upgrade)
    {
        if (packages.Length == 0)
            return;
        if (packages.Any(package => !model.CanQueue(package, upgrade)))
        {
            MessageBox.Show(this, "A selected package is installed, unresolved, ineligible, or based on stale inventory. Refresh and review it again.");
            return;
        }
        var targets = string.Join(Environment.NewLine, packages.Select(package => $"{package.Id}  {package.Available}  ({package.Source})"));
        if (MessageBox.Show(this, targets, upgrade ? "Confirm upgrade" : "Confirm install",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            await model.EnqueueAsync(packages, upgrade);
    }

    private void RemoveQueued(object sender, RoutedEventArgs e) => model.Remove(ActivityList.SelectedItem as ActivityEntry);

    private void CopyDetails(object sender, RoutedEventArgs e)
    {
        if (ActivityList.SelectedItem is ActivityEntry entry)
            Clipboard.SetText(entry.Display);
    }
}
