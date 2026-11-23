namespace WingetUI.Core;

public sealed record Package(string Name, string Id, string Source, string? Installed, string? Available, bool CanUpgrade)
{
    public bool MatchesInstalledVersion(string? currentVersion) =>
        Installed == (currentVersion ?? "Unknown");

    public string Eligibility => CanUpgrade ? "Update available" : Installed is null ? "Not installed" : "Installed";
}

public sealed record OperationProgress(string Phase, double? Percentage);
public sealed record OperationResult(string Status, string Detail);

public interface IPackageManager
{
    Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> ListInstalledAsync(CancellationToken cancellationToken);
    Task<OperationResult> ExecuteAsync(Package package, bool upgrade, IProgress<OperationProgress> progress, CancellationToken cancellationToken);
}

public sealed class PackageBackendUnavailableException(string message) : Exception(message);
