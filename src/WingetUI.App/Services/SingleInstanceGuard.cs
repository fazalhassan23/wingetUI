using System.Security.Principal;

namespace WingetUI.App.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex mutex;
    private bool ownsMutex;

    private SingleInstanceGuard(Mutex mutex, bool ownsMutex)
    {
        this.mutex = mutex;
        this.ownsMutex = ownsMutex;
    }

    public static SingleInstanceGuard Acquire(bool waitForPreviousInstance)
    {
        using var identity = WindowsIdentity.GetCurrent();
        var mutex = new Mutex(true, $@"Local\WingetUI-{identity.User!.Value}", out var ownsMutex);
        if (!ownsMutex && waitForPreviousInstance)
            ownsMutex = mutex.WaitOne(TimeSpan.FromSeconds(10));
        return new(mutex, ownsMutex);
    }

    public bool IsAcquired => ownsMutex;

    public void Dispose()
    {
        if (ownsMutex)
        {
            mutex.ReleaseMutex();
            ownsMutex = false;
        }
        mutex.Dispose();
    }
}
