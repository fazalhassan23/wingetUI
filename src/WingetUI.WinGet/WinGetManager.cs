using System.Runtime.InteropServices;
using Microsoft.Management.Deployment;
using WingetUI.Core;

namespace WingetUI.WinGet;

public sealed class WinGetManager : IPackageManager
{
    private const string Source = "winget";

    public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken) =>
        RunInMta<IReadOnlyList<Package>>(() => Read(query, cancellationToken), cancellationToken);

    public Task<IReadOnlyList<Package>> ListInstalledAsync(CancellationToken cancellationToken) =>
        RunInMta<IReadOnlyList<Package>>(() => Read(null, cancellationToken), cancellationToken);

    private static List<Package> Read(string? query, CancellationToken cancellationToken)
    {
        using var apartment = new ComApartment();
        var manager = Activate();
        var catalog = Connect(manager, installed: query is null);
        var options = new FindPackagesOptions();
        if (query is not null)
        {
            options.ResultLimit = 100;
            options.Selectors.Add(new PackageMatchFilter
            {
                Field = PackageMatchField.Id,
                Option = PackageFieldMatchOption.ContainsCaseInsensitive,
                Value = query
            });
            options.Selectors.Add(new PackageMatchFilter
            {
                Field = PackageMatchField.Name,
                Option = PackageFieldMatchOption.ContainsCaseInsensitive,
                Value = query
            });
        }

        var matches = Find(catalog, options);
        var inventory = query is null ? null : Find(Connect(manager, installed: true), new FindPackagesOptions());
        var installedById = new Dictionary<string, (string Version, bool CanUpgrade)>(StringComparer.OrdinalIgnoreCase);
        if (inventory is not null)
        {
            for (int index = 0; index < inventory.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = inventory[index].CatalogPackage;
                installedById[entry.Id] = (entry.InstalledVersion?.Version ?? "Unknown", entry.IsUpdateAvailable);
            }
        }

        var packages = new List<Package>(matches.Count);
        for (int index = 0; index < matches.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var package = matches[index].CatalogPackage;
            var installed = package.InstalledVersion;
            var available = package.DefaultInstallVersion;
            var correlated = installedById.GetValueOrDefault(package.Id);
            packages.Add(new Package(package.Name, package.Id, available is null ? "Unmatched" : Source,
                installed?.Version ?? (query is null ? "Unknown" : correlated.Version),
                available?.Version, query is null ? package.IsUpdateAvailable : correlated.CanUpgrade));
        }

        return packages;
    }

    public Task<OperationResult> ExecuteAsync(Package package, bool upgrade, IProgress<OperationProgress> progress,
        CancellationToken cancellationToken) => RunInMta(() =>
    {
        progress.Report(new("Resolving", null));
        cancellationToken.ThrowIfCancellationRequested();
        using var apartment = new ComApartment();
        var manager = Activate();
        var options = new FindPackagesOptions { ResultLimit = 2 };
        options.Filters.Add(new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.Equals,
            Value = package.Id
        });
        var matches = Find(Connect(manager, installed: upgrade), options);
        if (package.Source != Source || matches.Count != 1)
            return new OperationResult("Skipped", "The exact package could not be resolved.");
        var target = matches[0].CatalogPackage;
        var version = target.DefaultInstallVersion;
        if (package.Available is null || version?.Version != package.Available ||
            (upgrade && (!target.IsUpdateAvailable || target.InstalledVersion?.Version != package.Installed)) ||
            (!upgrade && Find(Connect(manager, installed: true), options).Count != 0))
            return new OperationResult("Skipped", "Package state changed. Refresh and confirm again.");

        cancellationToken.ThrowIfCancellationRequested();
        PackageVersionId? versionId = null;
        var versions = target.AvailableVersions;
        for (int index = 0; index < versions.Count; index++)
        {
            var candidate = versions[index];
            if (candidate.Version == package.Available)
            {
                if (versionId is not null)
                    return new OperationResult("Skipped", "The confirmed version matches multiple catalog entries.");
                versionId = candidate;
            }
        }
        if (versionId is null)
            return new OperationResult("Skipped", "The confirmed version is no longer available.");

        var installOptions = new InstallOptions
        {
            PackageInstallMode = PackageInstallMode.Interactive,
            PackageVersionId = versionId
        };
        progress.Report(new("Running", null));
        var operation = upgrade ? manager.UpgradePackageAsync(target, installOptions) :
            manager.InstallPackageAsync(target, installOptions);
        var result = operation.AsTask(cancellationToken, new Progress<InstallProgress>(value =>
        {
            string phase = value.State.ToString();
            double fraction = value.DownloadProgress;
            progress.Report(new(phase, phase == "Downloading" && fraction is > 0 and <= 1 ? fraction * 100 : null));
        })).GetAwaiter().GetResult();
        string status = result.RebootRequired ? "Reboot required" :
            result.Status == InstallResultStatus.Ok ? "Succeeded" : "Failed";
        return new OperationResult(status, $"{result.Status} (0x{result.ExtendedErrorCode?.HResult ?? 0:X8})");
    }, cancellationToken);

    private static IReadOnlyList<MatchResult> Find(PackageCatalog catalog, FindPackagesOptions options)
    {
        var result = catalog.FindPackages(options);
        if (result.Status != FindPackagesResultStatus.Ok)
            throw new InvalidOperationException($"WinGet search failed: {result.Status}");
        return result.Matches;
    }

    private static PackageCatalog Connect(PackageManager manager, bool installed)
    {
        var reference = manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog);
        if (installed)
        {
            var options = new CreateCompositePackageCatalogOptions { CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs };
            options.Catalogs.Add(reference);
            reference = manager.CreateCompositePackageCatalog(options);
        }

        var result = reference.Connect();
        if (result.Status != ConnectResultStatus.Ok)
            throw new InvalidOperationException($"WinGet community source failed to connect: {result.Status}");
        return result.PackageCatalog;
    }

    private static PackageManager Activate()
    {
        var clsid = new Guid("C53A4F16-787E-42A4-B304-29EFFB4BF597");
        var iid = new Guid("B375E3B9-F2E0-5C93-87A7-B67497F7E593");
        Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, IntPtr.Zero, 4, ref iid, out var pointer));
        try
        {
            return WinRT.MarshalInterface<PackageManager>.FromAbi(pointer);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private static Task<T> RunInMta<T>(Func<T> work, CancellationToken token)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                token.ThrowIfCancellationRequested();
                completion.SetResult(work());
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                completion.SetCanceled(token);
            }
            catch (Exception error)
            {
                completion.SetException(error);
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        return completion.Task;
    }

    private sealed class ComApartment : IDisposable
    {
        public ComApartment() => Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero, 0));

        public void Dispose() => CoUninitialize();
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoInitializeEx(IntPtr reserved, uint flags);

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void CoUninitialize();

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);
}
