namespace WingetUI.Core.Packages;

public sealed record Package(
    string Name,
    string Id,
    string Source,
    string? Installed,
    string? Available,
    bool CanUpgrade)
{
    public bool MatchesInstalledVersion(string? currentVersion) =>
        Installed == (currentVersion ?? "Unknown");

    public string Eligibility => CanUpgrade
        ? "Update available"
        : Installed is null
            ? "Not installed"
            : "Installed";
}
