using CommunityToolkit.Mvvm.Input;
using WingetUI.App.Models;

namespace WingetUI.App;

public partial class MainViewModel
{
    [RelayCommand(CanExecute = nameof(CanRefresh))]
    public async Task RefreshAsync()
    {
        inventoryCurrent = false;
        Status = "Refreshing WinGet inventory…";
        try
        {
            Installed = await backend.ListInstalledAsync(CancellationToken.None);
            Updates = Installed
                .Where(package => package.CanUpgrade && package.Source == "winget" && package.Available is not null)
                .Select(package => new UpdateRow(package)
                {
                    Operation = Activity.FirstOrDefault(entry =>
                        entry.Package.Id == package.Id && entry.Package.Source == package.Source)
                })
                .ToArray();
            inventoryCurrent = true;
            LastRefresh = $"Refreshed at {DateTime.Now:t}";
            Status = Updates.Count == 0 ? "No updates found." : $"{Updates.Count} updates available.";
        }
        catch (Exception error)
        {
            Status = $"Inventory unavailable. Displayed results may be stale. " +
                $"Check App Installer and the WinGet community source. (0x{error.HResult:X8})";
        }
    }
}
