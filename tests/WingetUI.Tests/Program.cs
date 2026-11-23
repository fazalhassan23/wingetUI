using System.IO;
using System.Text.Json;
using WingetUI.App;
using WingetUI.App.Models;
using WingetUI.App.Services;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;

var unknown = new Package("Unknown", "Test.Unknown", "winget", "Unknown", "2", true);
Require(unknown.MatchesInstalledVersion(null), "An unchanged unknown version must match.");
Require(!unknown.MatchesInstalledVersion("1"), "A newly known version requires confirmation.");
var known = new Package("Known", "Test.Known", "winget", "1", "2", true);
Require(!known.MatchesInstalledVersion("2"), "Version changes must be rejected.");
Console.WriteLine("PASS unknown and changed installed-version matching");

var tracker = new DownloadProgressTracker();
var observedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
tracker.Update(0, 10 * 1024 * 1024, observedAt);
var downloadMetrics = tracker.Update(1024 * 1024, 10 * 1024 * 1024, observedAt.AddSeconds(1));
Require(Math.Abs(downloadMetrics.BytesPerSecond - (1024 * 1024)) < 1,
    "Download speed must derive from transferred bytes and elapsed time.");
Require(downloadMetrics.EstimatedRemaining == TimeSpan.FromSeconds(9),
    "Download ETA must derive from remaining bytes and smoothed speed.");
var progressEntry = new ActivityEntry(known, true)
{
    Status = OperationStatuses.Running,
    Phase = "Downloading",
    ProgressIndeterminate = false,
    ProgressValue = 10,
    DownloadedBytes = downloadMetrics.DownloadedBytes,
    TotalBytes = downloadMetrics.TotalBytes,
    BytesPerSecond = downloadMetrics.BytesPerSecond,
    EstimatedRemaining = downloadMetrics.EstimatedRemaining
};
Require(progressEntry.ProgressDetail.Contains("1 MB/s") && progressEntry.ProgressDetail.Contains("9s left"),
    "Progress detail must expose readable speed and ETA.");
Console.WriteLine("PASS download speed, ETA, and progress formatting");

var directory = Path.Combine(Path.GetTempPath(), "WingetUI-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var path = Path.Combine(directory, "activity.json");
var packages = Enumerable.Range(0, 101).Select(index =>
    new Package($"Test {index}", $"Test.{index}", "winget", "1", "2", true)).ToArray();
var backend = new FakeBackend(packages);
var model = new MainViewModel(backend, path);
await model.RefreshAsync();
var execution = model.EnqueueAsync(packages, true);
var pending = JsonSerializer.Deserialize<ActivityEntry[]>(File.ReadAllText(path))!;
Require(pending.Length == 101 && pending.Any(entry => entry.Package.Id == "Test.0"),
    "All unfinished operations must survive persistence.");
backend.Release.SetResult();
await execution;
Require(backend.Executed == 101, "A package exception must not stop unrelated operations.");
Require(model.Updates[0].Operation?.Status == "Unknown", "Refresh must retain failed row outcomes.");
Require(model.Updates[1].Operation?.Status == "Succeeded", "Refresh must retain successful row outcomes.");
Console.WriteLine("PASS large-batch recovery, package-local failure continuation, retained row outcomes");

File.WriteAllText(path, """[null,{"Package":null,"Upgrade":true,"Status":"Queued"}]""");
new MainViewModel(backend, path).LoadHistory();
Console.WriteLine("PASS null history records");
var valid = new ActivityEntry(packages[0], true) { Status = "Running" };
File.WriteAllText(path, JsonSerializer.Serialize(new[] { valid }));
var recovered = new MainViewModel(backend, path);
recovered.LoadHistory();
Require(recovered.Activity.Single().Status == "Unknown", "Interrupted operations must recover as unknown.");
Console.WriteLine("PASS interrupted operation recovery");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class FakeBackend(Package[] packages) : IPackageManager
{
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Executed { get; private set; }
    public Task<IReadOnlyList<Package>> ListInstalledAsync(CancellationToken token) =>
        Task.FromResult<IReadOnlyList<Package>>(packages);
    public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken token) =>
        Task.FromResult<IReadOnlyList<Package>>([]);
    public async Task<OperationResult> ExecuteAsync(Package package, bool upgrade,
        IProgress<OperationProgress> progress, CancellationToken token)
    {
        Executed++;
        if (package.Id == "Test.0")
        {
            await Release.Task;
            throw new InvalidOperationException("Synthetic package failure");
        }
        return new("Succeeded", "Synthetic success");
    }
}
