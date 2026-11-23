using Microsoft.Management.Deployment;
using WingetUI.Core.Operations;
using WingetUI.Core.Packages;

namespace WingetUI.WinGet.Catalog;

internal sealed class WinGetCatalogClient(PackageManager manager)
{
    private const string Source = "winget";

    public IReadOnlyList<Package> Search(string query, CancellationToken cancellationToken)
    {
        var options = new FindPackagesOptions { ResultLimit = 100 };
        options.Selectors.Add(CreateContainsFilter(PackageMatchField.Id, query));
        options.Selectors.Add(CreateContainsFilter(PackageMatchField.Name, query));
        return MapPackages(Find(ConnectCommunityCatalog(), options), includeInstalledCorrelation: true,
            cancellationToken);
    }

    public IReadOnlyList<Package> ListInstalled(CancellationToken cancellationToken) =>
        MapPackages(Find(ConnectInstalledCatalog(), new FindPackagesOptions()),
            includeInstalledCorrelation: false, cancellationToken);

    public CatalogPackage? ResolveExact(Package package, bool installedCatalog)
    {
        var options = new FindPackagesOptions { ResultLimit = 2 };
        options.Filters.Add(new PackageMatchFilter
        {
            Field = PackageMatchField.Id,
            Option = PackageFieldMatchOption.Equals,
            Value = package.Id
        });
        var matches = Find(installedCatalog ? ConnectInstalledCatalog() : ConnectCommunityCatalog(), options);
        return package.Source == Source && matches.Count == 1 ? matches[0].CatalogPackage : null;
    }

    public bool IsInstalled(Package package) => ResolveExact(package, installedCatalog: true) is not null;

    private IReadOnlyList<Package> MapPackages(
        IReadOnlyList<MatchResult> matches,
        bool includeInstalledCorrelation,
        CancellationToken cancellationToken)
    {
        var installedById = includeInstalledCorrelation ? ReadInstalledCorrelation(cancellationToken) : null;
        var packages = new List<Package>(matches.Count);
        for (var index = 0; index < matches.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var catalogPackage = matches[index].CatalogPackage;
            var available = catalogPackage.DefaultInstallVersion;
            var correlated = installedById?.GetValueOrDefault(catalogPackage.Id);
            packages.Add(new(
                catalogPackage.Name,
                catalogPackage.Id,
                available is null ? "Unmatched" : Source,
                catalogPackage.InstalledVersion?.Version ?? (includeInstalledCorrelation ? correlated?.Version : "Unknown"),
                available?.Version,
                includeInstalledCorrelation ? correlated?.CanUpgrade == true : catalogPackage.IsUpdateAvailable));
        }
        return packages;
    }

    private Dictionary<string, InstalledState> ReadInstalledCorrelation(CancellationToken cancellationToken)
    {
        var matches = Find(ConnectInstalledCatalog(), new FindPackagesOptions());
        var installed = new Dictionary<string, InstalledState>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < matches.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var package = matches[index].CatalogPackage;
            installed[package.Id] = new(package.InstalledVersion?.Version ?? "Unknown", package.IsUpdateAvailable);
        }
        return installed;
    }

    private PackageCatalog ConnectInstalledCatalog()
    {
        var options = new CreateCompositePackageCatalogOptions
        {
            CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs
        };
        options.Catalogs.Add(manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog));
        return Connect(manager.CreateCompositePackageCatalog(options));
    }

    private PackageCatalog ConnectCommunityCatalog() =>
        Connect(manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog));

    private static PackageCatalog Connect(PackageCatalogReference reference)
    {
        var result = reference.Connect();
        if (result.Status != ConnectResultStatus.Ok)
            throw new PackageBackendUnavailableException($"WinGet community source failed to connect: {result.Status}");
        return result.PackageCatalog;
    }

    private static IReadOnlyList<MatchResult> Find(PackageCatalog catalog, FindPackagesOptions options)
    {
        var result = catalog.FindPackages(options);
        if (result.Status != FindPackagesResultStatus.Ok)
            throw new PackageBackendUnavailableException($"WinGet search failed: {result.Status}");
        return result.Matches;
    }

    private static PackageMatchFilter CreateContainsFilter(PackageMatchField field, string value) => new()
    {
        Field = field,
        Option = PackageFieldMatchOption.ContainsCaseInsensitive,
        Value = value
    };

    private sealed record InstalledState(string Version, bool CanUpgrade);
}
