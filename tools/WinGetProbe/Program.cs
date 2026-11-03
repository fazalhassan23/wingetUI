using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Management.Deployment;

internal static class Program
{
    [MTAThread]
    private static int Main()
    {
        using var identity = WindowsIdentity.GetCurrent();
        bool elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        Console.WriteLine($"Windows {Environment.OSVersion.Version}, {RuntimeInformation.ProcessArchitecture}, elevated={elevated}");

        try
        {
            var clsid = new Guid("C53A4F16-787E-42A4-B304-29EFFB4BF597");
            var iid = new Guid("B375E3B9-F2E0-5C93-87A7-B67497F7E593");
            const uint localServer = 4;
            Marshal.ThrowExceptionForHR(CoCreateInstance(ref clsid, IntPtr.Zero, localServer, ref iid, out var pointer));
            PackageManager manager;
            try
            {
                manager = WinRT.MarshalInterface<PackageManager>.FromAbi(pointer);
            }
            finally
            {
                Marshal.Release(pointer);
            }

            var result = manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog).Connect();
            Console.WriteLine($"Community catalog connection: {result.Status}");
            if (result.Status != ConnectResultStatus.Ok)
                return 1;

            var search = new FindPackagesOptions { ResultLimit = 5 };
            search.Selectors.Add(new PackageMatchFilter
            {
                Field = PackageMatchField.Id,
                Option = PackageFieldMatchOption.Equals,
                Value = "Microsoft.PowerToys"
            });
            var found = result.PackageCatalog.FindPackages(search);
            Console.WriteLine($"Exact catalog search: {found.Status}, matches={found.Matches.Count}");
            for (int index = 0; index < found.Matches.Count; index++)
                Console.WriteLine($"  {found.Matches[index].CatalogPackage.Id}");

            var composite = new CreateCompositePackageCatalogOptions
            {
                CompositeSearchBehavior = CompositeSearchBehavior.LocalCatalogs
            };
            composite.Catalogs.Add(manager.GetPredefinedPackageCatalog(PredefinedPackageCatalog.OpenWindowsCatalog));
            var installedCatalog = manager.CreateCompositePackageCatalog(composite).Connect();
            Console.WriteLine($"Installed catalog connection: {installedCatalog.Status}");
            if (installedCatalog.Status != ConnectResultStatus.Ok)
                return 1;

            var installed = installedCatalog.PackageCatalog.FindPackages(new FindPackagesOptions());
            Console.WriteLine($"Installed search: {installed.Status}, matches={installed.Matches.Count}");
            int updates = 0;
            for (int index = 0; index < installed.Matches.Count; index++)
            {
                if (installed.Matches[index].CatalogPackage.IsUpdateAvailable)
                    updates++;
            }

            Console.WriteLine($"WinGet reports updates for {updates} installed entries.");
            return found.Status == FindPackagesResultStatus.Ok && found.Matches.Count == 1 &&
                   installed.Status == FindPackagesResultStatus.Ok ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"{error.GetType().Name}: 0x{error.HResult:X8}");
            return 1;
        }
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context, ref Guid iid, out IntPtr instance);
}
