using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Management.Deployment;

namespace WingetUI.WinGet.Activation;

internal static class WinGetActivationFactory
{
    private static readonly Guid PackageManagerClassId = new("C53A4F16-787E-42A4-B304-29EFFB4BF597");
    private static readonly Guid PackageManagerInterfaceId = new("B375E3B9-F2E0-5C93-87A7-B67497F7E593");
    private const uint LocalServer = 0x4;

    public static PackageManager CreatePackageManager()
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            return new PackageManager();

        var classId = PackageManagerClassId;
        var interfaceId = PackageManagerInterfaceId;
        var result = CoCreateInstance(ref classId, IntPtr.Zero, LocalServer, ref interfaceId, out var pointer);
        Marshal.ThrowExceptionForHR(result);
        try
        {
            return WinRT.MarshalInterface<PackageManager>.FromAbi(pointer);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoCreateInstance(ref Guid clsid, IntPtr outer, uint context,
        ref Guid iid, out IntPtr instance);
}
