using WingetUI.Core.Packages;

namespace WingetUI.Core.Operations;

public interface IPackageManager
{
    Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<IReadOnlyList<Package>> ListInstalledAsync(CancellationToken cancellationToken);
    Task<OperationResult> ExecuteAsync(Package package, bool upgrade,
        IProgress<OperationProgress> progress, CancellationToken cancellationToken);
}
