using System.Security.Principal;
using Microsoft.Management.Deployment;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;
using WingetUI.WinGet.Catalog;

namespace WingetUI.WinGet.Operations;

internal sealed class WinGetOperationExecutor(PackageManager manager, WinGetCatalogClient catalog)
{
    public OperationResult Execute(Package package, bool upgrade,
        IProgress<OperationProgress> progress, CancellationToken cancellationToken)
    {
        progress.Report(new(OperationStatuses.Resolving, null));
        cancellationToken.ThrowIfCancellationRequested();

        var target = catalog.ResolveExact(package, installedCatalog: upgrade);
        if (target is null)
            return new(OperationStatuses.Skipped, "The exact package could not be resolved.");

        var availableVersion = target.DefaultInstallVersion;
        if (StateChanged(package, target, availableVersion, upgrade))
            return new(OperationStatuses.Skipped, "Package state changed. Refresh and confirm again.");

        var versionId = ResolveVersionId(target, package.Available!);
        if (versionId is null)
            return new(OperationStatuses.Skipped, "The confirmed version is no longer uniquely available.");

        var options = new InstallOptions
        {
            PackageInstallMode = IsElevated() ? PackageInstallMode.Silent : PackageInstallMode.Interactive,
            PackageVersionId = versionId
        };
        progress.Report(new(OperationStatuses.Running, null));
        var operation = upgrade
            ? manager.UpgradePackageAsync(target, options)
            : manager.InstallPackageAsync(target, options);
        var result = operation.AsTask(cancellationToken, new Progress<InstallProgress>(value =>
        {
            var phase = value.State.ToString();
            double? percentage = phase switch
            {
                "Downloading" when value.DownloadProgress is >= 0 and <= 1 => value.DownloadProgress * 100,
                "Installing" when value.InstallationProgress is >= 0 and <= 1 => value.InstallationProgress * 100,
                _ => null
            };
            progress.Report(new(phase, percentage, value.BytesDownloaded, value.BytesRequired));
        })).GetAwaiter().GetResult();

        var status = result.RebootRequired
            ? OperationStatuses.RebootRequired
            : result.Status == InstallResultStatus.Ok
                ? OperationStatuses.Succeeded
                : OperationStatuses.Failed;
        return new(status, $"{result.Status} (0x{result.ExtendedErrorCode?.HResult ?? 0:X8})");
    }

    private bool StateChanged(Package package, CatalogPackage target,
        PackageVersionInfo? availableVersion, bool upgrade) =>
        package.Available is null || availableVersion?.Version != package.Available ||
        (upgrade && (!target.IsUpdateAvailable || !package.MatchesInstalledVersion(target.InstalledVersion?.Version))) ||
        (!upgrade && catalog.IsInstalled(package));

    private static PackageVersionId? ResolveVersionId(CatalogPackage target, string confirmedVersion)
    {
        PackageVersionId? resolved = null;
        var versions = target.AvailableVersions;
        for (var index = 0; index < versions.Count; index++)
        {
            var candidate = versions[index];
            if (candidate.Version != confirmedVersion)
                continue;
            if (resolved is not null)
                return null;
            resolved = candidate;
        }
        return resolved;
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
