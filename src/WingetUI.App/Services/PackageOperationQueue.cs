using System.Collections.ObjectModel;
using System.IO;
using WingetUI.App.Models;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;

namespace WingetUI.App.Services;

public sealed record QueueProgress(ActivityEntry Entry, int Completed, int Waiting);
public sealed record QueueSubmission(IReadOnlyList<ActivityEntry> Added, Task Completion, bool StartedProcessing);

public sealed class PackageOperationQueue(IPackageManager backend, IActivityHistoryStore historyStore)
{
    private readonly object gate = new();
    private readonly Queue<ActivityEntry> pending = new();
    private Task? processingTask;

    public ObservableCollection<ActivityEntry> Activity { get; } = [];
    public bool IsBusy { get; private set; }
    public event Action<QueueProgress>? ProgressChanged;
    public event Action? PersistenceFailed;

    public void LoadHistory()
    {
        foreach (var entry in historyStore.Load())
        {
            if (PackageOperationPolicy.IsPending(entry.Status))
            {
                entry.Status = OperationStatuses.Unknown;
                entry.Detail = "Interrupted. Check the refreshed inventory before retrying.";
            }
            Activity.Add(entry);
        }
        Persist();
    }

    public bool IsDuplicate(Package package) => Activity.Any(entry =>
        entry.Package.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) &&
        entry.Package.Source == package.Source && PackageOperationPolicy.IsPending(entry.Status));

    public void Remove(ActivityEntry? entry)
    {
        if (entry?.Status != OperationStatuses.Queued)
            return;

        entry.Finished = DateTimeOffset.Now;
        entry.Status = OperationStatuses.Cancelled;
        entry.Detail = "Removed before execution.";
        Persist();
    }

    public QueueSubmission Enqueue(
        IEnumerable<Package> packages,
        bool upgrade,
        bool inventoryCurrent)
    {
        var added = new List<ActivityEntry>();
        Task completion;
        var startedProcessing = false;
        lock (gate)
        {
            foreach (var package in packages)
            {
                if (!PackageOperationPolicy.CanQueue(package, upgrade, inventoryCurrent) || IsDuplicate(package))
                    continue;

                var entry = new ActivityEntry(package, upgrade);
                Activity.Insert(0, entry);
                pending.Enqueue(entry);
                added.Add(entry);
            }
            Persist();
            if (processingTask is null && pending.Count > 0)
            {
                processingTask = ProcessAsync();
                startedProcessing = true;
            }
            completion = processingTask ?? Task.CompletedTask;
        }
        return new(added, completion, startedProcessing);
    }

    private async Task ProcessAsync()
    {
        await Task.Yield();
        IsBusy = true;
        var completed = 0;
        try
        {
            while (true)
            {
                ActivityEntry? entry;
                lock (gate)
                {
                    if (!pending.TryDequeue(out entry))
                    {
                        processingTask = null;
                        return;
                    }
                }
                if (entry.Status != OperationStatuses.Queued)
                    continue;

                await ExecuteEntryAsync(entry);
                completed++;
                ProgressChanged?.Invoke(new(entry, completed,
                    pending.Count(candidate => candidate.Status == OperationStatuses.Queued)));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteEntryAsync(ActivityEntry entry)
    {
        entry.Started = DateTimeOffset.Now;
        entry.Status = OperationStatuses.Resolving;
        Persist();

        var progress = new Progress<OperationProgress>(update =>
        {
            if (entry.Status is not (OperationStatuses.Resolving or OperationStatuses.Running))
                return;

            entry.Phase = update.Phase;
            entry.ProgressValue = update.Percentage ?? 0;
            entry.ProgressIndeterminate = update.Percentage is null;
            if (update.Phase != OperationStatuses.Resolving && entry.Status != OperationStatuses.Running)
            {
                entry.Status = OperationStatuses.Running;
                Persist();
            }
            ProgressChanged?.Invoke(new(entry, 0, pending.Count));
        });

        try
        {
            var result = await backend.ExecuteAsync(entry.Package, entry.Upgrade, progress, CancellationToken.None);
            entry.Status = result.Status;
            entry.Detail = result.Detail;
        }
        catch (OperationCanceledException)
        {
            entry.Status = OperationStatuses.Cancelled;
            entry.Detail = "WinGet reported cancellation.";
        }
        catch (Exception error)
        {
            entry.Status = OperationStatuses.Unknown;
            entry.Detail = $"WinGet call failed (0x{error.HResult:X8}). Check inventory before retrying.";
            if (error is PackageBackendUnavailableException)
                SkipPendingEntries();
        }

        entry.Finished = DateTimeOffset.Now;
        Persist();
    }

    private void SkipPendingEntries()
    {
        foreach (var entry in pending.Where(entry => entry.Status == OperationStatuses.Queued))
        {
            entry.Finished = DateTimeOffset.Now;
            entry.Status = OperationStatuses.Skipped;
            entry.Detail = "Queue stopped after a backend failure.";
        }
    }

    private void Persist()
    {
        try
        {
            historyStore.Save(Activity);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            PersistenceFailed?.Invoke();
        }
    }
}
