using WingetUI.App.Models;
using WingetUI.App.Services;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;

namespace WingetUI.App;

public partial class MainViewModel
{
    public bool CanQueue(Package package, bool upgrade) =>
        PackageOperationPolicy.CanQueue(package, upgrade, inventoryCurrent);

    public void Remove(ActivityEntry? entry) => operationQueue.Remove(entry);

    public async Task EnqueueAsync(IEnumerable<Package> packages, bool upgrade)
    {
        var submission = operationQueue.Enqueue(packages, upgrade, inventoryCurrent);
        foreach (var entry in submission.Added)
            foreach (var row in Updates.Where(row => row.Package == entry.Package))
                row.Operation = entry;

        if (submission.Added.Count == 0 || !submission.StartedProcessing)
            return;

        Busy = true;
        try
        {
            await submission.Completion;
            await RefreshAsync();
        }
        finally
        {
            Busy = false;
        }
    }

    private void HandleQueueProgress(QueueProgress progress)
    {
        if (progress.Entry.ShowProgress)
        {
            Indeterminate = progress.Entry.ProgressIndeterminate;
            Percentage = progress.Entry.ProgressValue;
            OperationStatus = $"{progress.Entry.Package.Name}: {progress.Entry.RowStatus}";
            return;
        }

        Indeterminate = false;
        Percentage = 0;
        OperationStatus = $"{progress.Completed} completed, {progress.Waiting} waiting. " +
            $"Last: {progress.Entry.Status}.";
    }
}
