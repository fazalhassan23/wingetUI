using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WingetUI.Core;

namespace WingetUI.App;

public partial class MainViewModel(IPackageManager backend) : ObservableObject
{
    private readonly Queue<ActivityEntry> queue = new();
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WingetUI", "activity.json");
    private CancellationTokenSource? search;
    private bool inventoryCurrent;

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
    public ObservableCollection<ActivityEntry> Activity { get; } = [];

    private bool CanRefresh() => !Busy;

    public void LoadHistory()
    {
        try
        {
            if (!File.Exists(HistoryPath))
                return;
            var entries = JsonSerializer.Deserialize<ActivityEntry[]>(File.ReadAllText(HistoryPath)) ?? [];
            foreach (var entry in entries.OrderByDescending(entry => entry.Created).Take(100))
            {
                if (entry.Status is "Queued" or "Resolving" or "Running")
                {
                    entry.Status = "Unknown";
                    entry.Detail = "Interrupted. Check the refreshed inventory before retrying.";
                }
                Activity.Add(entry);
            }
            SaveHistory();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            OperationStatus = "Activity history could not be loaded.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    public async Task RefreshAsync()
    {
        inventoryCurrent = false;
        Status = "Refreshing WinGet inventory…";
        try
        {
            Installed = await backend.ListInstalledAsync(CancellationToken.None);
            Updates = Installed.Where(package => package.CanUpgrade && package.Source == "winget" && package.Available is not null)
                .Select(package => new UpdateRow(package)).ToArray();
            inventoryCurrent = true;
            LastRefresh = $"Refreshed at {DateTime.Now:t}";
            Status = Updates.Count == 0 ? "No updates found." : $"{Updates.Count} updates available.";
        }
        catch (Exception error)
        {
            Status = $"Inventory unavailable. Displayed results may be stale. Check App Installer and the WinGet community source. (0x{error.HResult:X8})";
        }
    }

    partial void OnQueryChanged(string value)
    {
        search?.Cancel();
        SearchResults = [];
        if (!string.IsNullOrWhiteSpace(value))
            _ = SearchAsync(value.Trim());
    }

    private async Task SearchAsync(string value)
    {
        using var request = new CancellationTokenSource();
        search = request;
        try
        {
            await Task.Delay(300, request.Token);
            Status = "Searching the WinGet community source…";
            var results = await backend.SearchAsync(value, request.Token);
            if (!request.IsCancellationRequested)
            {
                SearchResults = results;
                Status = results.Count == 0 ? "No matching packages." : $"{results.Count} matches.";
            }
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!request.IsCancellationRequested)
                Status = $"WinGet search failed (0x{error.HResult:X8}). Check App Installer and try again.";
        }
        finally
        {
            if (search == request)
                search = null;
        }
    }

    public bool CanQueue(Package package, bool upgrade) =>
        inventoryCurrent && package.Source == "winget" && package.Available is not null &&
        (upgrade ? package.CanUpgrade : package.Installed is null);

    public void Remove(ActivityEntry? entry)
    {
        if (entry?.Status != "Queued")
            return;
        entry.Status = "Cancelled";
        entry.Detail = "Removed before execution.";
        SaveHistory();
    }

    public async Task EnqueueAsync(IEnumerable<Package> packages, bool upgrade)
    {
        foreach (var package in packages)
        {
            if (!CanQueue(package, upgrade) || Activity.Any(entry =>
                entry.Package.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) &&
                entry.Package.Source == package.Source && entry.Status is "Queued" or "Resolving" or "Running"))
                continue;
            var item = new ActivityEntry(package, upgrade);
            Activity.Insert(0, item);
            foreach (var row in Updates.Where(row => row.Package == package))
                row.Operation = item;
            queue.Enqueue(item);
        }
        SaveHistory();

        if (Busy || queue.Count == 0)
            return;
        Busy = true;
        int completed = 0;
        try
        {
            while (queue.TryDequeue(out var item))
            {
                if (item.Status != "Queued")
                    continue;
                item.Started = DateTimeOffset.Now;
                item.Status = "Resolving";
                Indeterminate = true;
                SaveHistory();
                try
                {
                    var progress = new Progress<OperationProgress>(update =>
                    {
                        if (item.Status is not ("Resolving" or "Running"))
                            return;
                        item.Phase = update.Phase;
                        item.ProgressValue = update.Percentage ?? 0;
                        item.ProgressIndeterminate = update.Percentage is null;
                        if (update.Phase != "Resolving" && item.Status != "Running")
                        {
                            item.Status = "Running";
                            SaveHistory();
                        }
                        Indeterminate = update.Percentage is null;
                        Percentage = update.Percentage ?? 0;
                        OperationStatus = $"{item.Package.Name}: {update.Phase}";
                    });
                    var result = await backend.ExecuteAsync(item.Package, item.Upgrade, progress, CancellationToken.None);
                    item.Status = result.Status;
                    item.Detail = result.Detail;
                }
                catch (Exception error)
                {
                    item.Status = "Unknown";
                    item.Detail = $"WinGet call failed (0x{error.HResult:X8}). Check inventory before retrying.";
                    foreach (var pending in queue.Where(pending => pending.Status == "Queued"))
                    {
                        pending.Status = "Skipped";
                        pending.Detail = "Queue stopped after a backend failure.";
                    }
                }
                item.Finished = DateTimeOffset.Now;
                completed++;
                SaveHistory();
                Indeterminate = false;
                Percentage = 0;
                OperationStatus = $"{completed} completed, {queue.Count(entry => entry.Status == "Queued")} waiting. Last: {item.Status}.";
            }
            await RefreshAsync();
        }
        finally
        {
            Busy = false;
        }
    }

    private void SaveHistory()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
            var temporary = HistoryPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(Activity.Take(100)));
            File.Move(temporary, HistoryPath, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            OperationStatus = "Activity history could not be saved.";
        }
    }
}

public partial class ActivityEntry(Package package, bool upgrade) : ObservableObject
{
    public Package Package { get; } = package;
    public bool Upgrade { get; } = upgrade;
    public DateTimeOffset Created { get; init; } = DateTimeOffset.Now;
    [ObservableProperty] private string status = "Queued";
    [ObservableProperty] private string detail = "";
    public string Action => Upgrade ? "Upgrade" : "Install";
    public DateTimeOffset? Started { get; set; }
    public DateTimeOffset? Finished { get; set; }
    [ObservableProperty, NotifyPropertyChangedFor(nameof(RowStatus)), NotifyPropertyChangedFor(nameof(ShowProgress))]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private string phase = "";
    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private double progressValue;
    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private bool progressIndeterminate = true;
    public bool ShowProgress => Status is "Resolving" or "Running";
    public string RowStatus => ShowProgress && !string.IsNullOrEmpty(Phase) ? Phase : Status;
    public string Display =>
        $"{Action} {Package.Name} ({Package.Id}) [{Package.Source}] {Package.Installed ?? "—"} → {Package.Available ?? "—"}\n" +
        $"Queued {Created:g} | Started {Started:g} | Finished {Finished:g}\n{Status}: {Detail}";

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(RowStatus));
        OnPropertyChanged(nameof(ShowProgress));
    }
    partial void OnDetailChanged(string value) => OnPropertyChanged(nameof(Display));
}

public partial class UpdateRow(Package package) : ObservableObject
{
    public Package Package { get; } = package;

    [ObservableProperty] private bool isChecked;
    [ObservableProperty] private ActivityEntry? operation;
}
