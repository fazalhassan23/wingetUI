using CommunityToolkit.Mvvm.ComponentModel;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;

namespace WingetUI.App.Models;

public partial class ActivityEntry(Package package, bool upgrade) : ObservableObject
{
    public Package Package { get; } = package;
    public bool Upgrade { get; } = upgrade;
    public DateTimeOffset Created { get; init; } = DateTimeOffset.Now;
    public string Action => Upgrade ? "Upgrade" : "Install";

    [ObservableProperty] private string status = OperationStatuses.Queued;
    [ObservableProperty] private string detail = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))] private DateTimeOffset? started;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))] private DateTimeOffset? finished;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(RowStatus)), NotifyPropertyChangedFor(nameof(ShowProgress))]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private string phase = "";

    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private double progressValue;

    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private bool progressIndeterminate = true;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProgressDetail))]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private ulong downloadedBytes;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProgressDetail))]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private ulong totalBytes;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProgressDetail))]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private double bytesPerSecond;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProgressDetail))]
    [property: System.Text.Json.Serialization.JsonIgnore]
    private TimeSpan? estimatedRemaining;

    public bool ShowProgress => PackageOperationPolicy.IsPending(Status) && Status != OperationStatuses.Queued;
    public string RowStatus => ShowProgress && !string.IsNullOrEmpty(Phase) ? Phase : Status;
    public string ProgressDetail
    {
        get
        {
            if (!ShowProgress)
                return Detail;

            var parts = new List<string>();
            if (!ProgressIndeterminate)
                parts.Add($"{ProgressValue:0}%");
            if (Phase == "Downloading" && DownloadedBytes > 0)
            {
                var transfer = FormatBytes(DownloadedBytes);
                if (TotalBytes > 0)
                    transfer += $" / {FormatBytes(TotalBytes)}";
                parts.Add(transfer);
            }
            if (Phase == "Downloading" && BytesPerSecond >= 1024)
                parts.Add($"{FormatBytes((ulong)BytesPerSecond)}/s");
            if (Phase == "Downloading" && EstimatedRemaining is { TotalSeconds: > 0 } remaining)
                parts.Add($"{FormatDuration(remaining)} left");
            return parts.Count == 0 ? "Working…" : string.Join(" · ", parts);
        }
    }
    public string Display =>
        $"{Action} {Package.Name} ({Package.Id}) [{Package.Source}] {Package.Installed ?? "—"} → {Package.Available ?? "—"}\n" +
        $"Queued {Created:g} | Started {Started:g} | Finished {Finished:g}\n{Status}: {Detail}";

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(RowStatus));
        OnPropertyChanged(nameof(ShowProgress));
        OnPropertyChanged(nameof(ProgressDetail));
    }

    partial void OnDetailChanged(string value) => OnPropertyChanged(nameof(Display));
    partial void OnPhaseChanged(string value) => OnPropertyChanged(nameof(ProgressDetail));
    partial void OnProgressValueChanged(double value) => OnPropertyChanged(nameof(ProgressDetail));
    partial void OnProgressIndeterminateChanged(bool value) => OnPropertyChanged(nameof(ProgressDetail));

    private static string FormatBytes(ulong bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.#} {units[unit]}";
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalMinutes >= 1
        ? $"{Math.Ceiling(duration.TotalMinutes):0}m"
        : $"{Math.Ceiling(duration.TotalSeconds):0}s";
}
