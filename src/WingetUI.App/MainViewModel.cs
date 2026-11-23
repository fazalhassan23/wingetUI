using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WingetUI.App.Models;
using WingetUI.App.Services;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;

namespace WingetUI.App;

public partial class MainViewModel : ObservableObject
{
    private readonly IPackageManager backend;
    private readonly PackageOperationQueue operationQueue;
    private CancellationTokenSource? search;
    private bool inventoryCurrent;

    public MainViewModel(IPackageManager backend, string? historyPath = null)
    {
        this.backend = backend;
        var resolvedHistoryPath = historyPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WingetUI", "activity.json");
        operationQueue = new PackageOperationQueue(backend, new JsonActivityHistoryStore(resolvedHistoryPath));
        operationQueue.ProgressChanged += HandleQueueProgress;
        operationQueue.PersistenceFailed += () => OperationStatus = "Activity history could not be saved.";
    }

    [ObservableProperty] private string query = "";
    [ObservableProperty] private string status = "Loading WinGet inventory…";
    [ObservableProperty] private string operationStatus = "";
    [ObservableProperty] private string lastRefresh = "";
    [ObservableProperty] private double percentage;
    [ObservableProperty] private bool indeterminate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool busy;

    [ObservableProperty] private IReadOnlyList<Package> installed = [];
    [ObservableProperty] private IReadOnlyList<UpdateRow> updates = [];
    [ObservableProperty] private IReadOnlyList<Package> searchResults = [];

    public ObservableCollection<ActivityEntry> Activity => operationQueue.Activity;

    private bool CanRefresh() => !Busy;

    public void LoadHistory()
    {
        try
        {
            operationQueue.LoadHistory();
        }
        catch (Exception error) when (
            error is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            OperationStatus = "Activity history could not be loaded.";
        }
    }
}
