using System.Security.Principal;
using System.Windows;

namespace WingetUI.App;

public partial class App : Application
{
    private Mutex? instance;
    private bool ownsInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        using var identity = WindowsIdentity.GetCurrent();
        instance = new Mutex(true, $@"Local\WingetUI-{identity.User!.Value}", out ownsInstance);
        if (!ownsInstance)
        {
            MessageBox.Show("WingetUI is already running for this account.");
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ownsInstance)
            instance?.ReleaseMutex();
        instance?.Dispose();
        base.OnExit(e);
    }
}
