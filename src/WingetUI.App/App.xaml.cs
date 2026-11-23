using System.Windows;
using WingetUI.App.Services;

namespace WingetUI.App;

public partial class App : Application
{
    private SingleInstanceGuard? instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        instance = SingleInstanceGuard.Acquire(e.Args.Contains("--elevated-restart"));
        if (!instance.IsAcquired)
        {
            MessageBox.Show("WingetUI is already running for this account.");
            Shutdown();
            return;
        }
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        instance?.Dispose();
        base.OnExit(e);
    }
}
