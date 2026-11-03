using WingetUI.WinGet;

if (args.Contains("--check-in-process"))
{
    var inProcess = new Microsoft.Management.Deployment.PackageManager();
    var catalog = inProcess.GetPredefinedPackageCatalog(Microsoft.Management.Deployment.PredefinedPackageCatalog.OpenWindowsCatalog).Connect();
    if (catalog.Status != Microsoft.Management.Deployment.ConnectResultStatus.Ok)
        throw new InvalidOperationException($"In-process community catalog connection failed: {catalog.Status}.");
    Console.WriteLine("In-process community catalog connected.");
}

var manager = new WinGetManager();
var results = await manager.SearchAsync("Microsoft.PowerToys", CancellationToken.None);
var match = results.Single(package => package.Id == "Microsoft.PowerToys");
if (match.Source != "winget" || string.IsNullOrWhiteSpace(match.Available))
    throw new InvalidOperationException("The known package had no resolved community version.");

var inventory = await manager.ListInstalledAsync(CancellationToken.None);
if (inventory.Count == 0)
    throw new InvalidOperationException("Expected installed entries on the smoke-test host.");

Console.WriteLine($"Search matched {match.Id} {match.Available}. Inventory: {inventory.Count}; update candidates: {inventory.Count(package => package.CanUpgrade)}.");

var stale = match with { Available = "smoke-test-unavailable-version" };
var result = await manager.ExecuteAsync(stale, upgrade: false, new Progress<WingetUI.Core.OperationProgress>(), CancellationToken.None);
if (result.Status != "Skipped")
    throw new InvalidOperationException("A stale confirmed version must be skipped.");
Console.WriteLine("Stale version was skipped before installer execution.");
