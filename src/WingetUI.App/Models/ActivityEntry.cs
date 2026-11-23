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

    public bool ShowProgress => PackageOperationPolicy.IsPending(Status) && Status != OperationStatuses.Queued;
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
