using System.Windows;
using WingetUI.App.Models;
using WingetUI.App.Services;
using WingetUI.Core.Packages;
using WingetUI.WinGet;

namespace WingetUI.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel model = new(new WinGetManager());
    private ThemeManager theme = null!;

    public MainWindow()
    {
        InitializeComponent();
        theme = new ThemeManager(Resources);
        if (SystemParameters.HighContrast)
            theme.ApplySystemTheme();
        DataContext = model;
        model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Updates))
                UpdateSelectionCount();
        };
        if (AdministratorSession.IsElevated)
        {
            ElevateButton.Visibility = Visibility.Collapsed;
            Title = "WingetUI (Administrator)";
        }
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

    private void ToggleTheme(object sender, RoutedEventArgs e) => theme.Toggle();

    private void RestartAsAdministrator(object sender, RoutedEventArgs e)
    {
        if (model.Busy)
        {
            MessageBox.Show(this, "Wait for the active queue to finish before restarting.");
            return;
        }

        if (MessageBox.Show(this,
                "Restart WingetUI with administrator privileges and request silent installers where supported? " +
                "The app will refresh and ask you to select and confirm your packages again. " +
                "Using another Windows account can change the packages and sources you see.",
                "Restart as administrator", MessageBoxButton.YesNo, MessageBoxImage.Question,
                MessageBoxResult.No) != MessageBoxResult.Yes)
            return;

        var outcome = AdministratorSession.RestartElevated();
        switch (outcome.Result)
        {
            case AdministratorRestartResult.Started:
                Close();
                break;
            case AdministratorRestartResult.Cancelled:
                MessageBox.Show(this, "Administrator restart was cancelled.");
                break;
            default:
                MessageBox.Show(this, $"Administrator restart failed (Windows error {outcome.WindowsError ?? 0}).");
                break;
        }
    }

    private void SelectAllUpdates(object sender, RoutedEventArgs e) => SetUpdateChecks(true);

    private void SelectAllCheckboxClicked(object sender, RoutedEventArgs e) =>
        SetUpdateChecks(!model.Updates.All(row => row.IsChecked));

    private void ClearUpdates(object sender, RoutedEventArgs e) => SetUpdateChecks(false);

    private void SetUpdateChecks(bool value)
    {
        foreach (var row in model.Updates)
            row.IsChecked = value;
        UpdateSelectionCount();
    }

    private void UpdateCheckboxClicked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox { DataContext: UpdateRow row } checkbox)
            row.IsChecked = checkbox.IsChecked == true;
        UpdateSelectionCount();
    }

    private void UpdateSelectionCount()
    {
        int count = model.Updates.Count(row => row.IsChecked);
        SelectAllCheckbox.IsEnabled = model.Updates.Count > 0;
        SelectAllCheckbox.IsChecked = count == 0 ? false :
            count == model.Updates.Count ? true : null;
        UpgradeButton.Content = $"_Upgrade selected ({count})";
        UpgradeButton.IsEnabled = count > 0;
    }

    private async void UpgradeSelected(object sender, RoutedEventArgs e) =>
        await ConfirmAsync(model.Updates.Where(row => row.IsChecked).Select(row => row.Package).ToArray(), upgrade: true);

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
