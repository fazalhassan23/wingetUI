using WingetUI.Core.Operations;
using WingetUI.Core.Packages;
using WingetUI.WinGet.Activation;
using WingetUI.WinGet.Catalog;
using WingetUI.WinGet.Operations;

namespace WingetUI.WinGet;

public sealed class WinGetManager : IPackageManager
{
    public Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken cancellationToken) =>
        MtaThreadRunner.RunAsync(
            () => WithCatalog(client => client.Search(query, cancellationToken)),
            cancellationToken);

    public Task<IReadOnlyList<Package>> ListInstalledAsync(CancellationToken cancellationToken) =>
        MtaThreadRunner.RunAsync(
            () => WithCatalog(client => client.ListInstalled(cancellationToken)),
            cancellationToken);

    public Task<OperationResult> ExecuteAsync(Package package, bool upgrade,
        IProgress<OperationProgress> progress, CancellationToken cancellationToken) =>
        MtaThreadRunner.RunAsync(() =>
        {
            using var apartment = new ComApartment();
            var manager = WinGetActivationFactory.CreatePackageManager();
            var catalog = new WinGetCatalogClient(manager);
            return new WinGetOperationExecutor(manager, catalog)
                .Execute(package, upgrade, progress, cancellationToken);
        }, cancellationToken);

    private static T WithCatalog<T>(Func<WinGetCatalogClient, T> action)
    {
        using var apartment = new ComApartment();
        var manager = WinGetActivationFactory.CreatePackageManager();
        return action(new WinGetCatalogClient(manager));
    }
}
