using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;

namespace WingetUI.App.Services;

public enum AdministratorRestartResult { Started, Cancelled, Failed }

public sealed record AdministratorRestartOutcome(AdministratorRestartResult Result, int? WindowsError = null);

public static class AdministratorSession
{
    public static bool IsElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public static AdministratorRestartOutcome RestartElevated()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(Environment.ProcessPath!)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "--elevated-restart"
            });
            return process is null ? new(AdministratorRestartResult.Failed) : new(AdministratorRestartResult.Started);
        }
        catch (Win32Exception error) when (error.NativeErrorCode == 1223)
        {
            return new(AdministratorRestartResult.Cancelled, error.NativeErrorCode);
        }
        catch (Win32Exception error)
        {
            return new(AdministratorRestartResult.Failed, error.NativeErrorCode);
        }
    }
}
