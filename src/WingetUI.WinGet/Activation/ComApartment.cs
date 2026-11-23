using System.Runtime.InteropServices;

namespace WingetUI.WinGet.Activation;

internal sealed class ComApartment : IDisposable
{
    public ComApartment() => Marshal.ThrowExceptionForHR(CoInitializeEx(IntPtr.Zero, 0));
    public void Dispose() => CoUninitialize();

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoInitializeEx(IntPtr reserved, uint flags);

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void CoUninitialize();
}
