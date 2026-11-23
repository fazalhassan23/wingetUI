using WingetUI.Core.Packages;

namespace WingetUI.Core.Operations;

public static class PackageOperationPolicy
{
    public static bool CanQueue(Package package, bool upgrade, bool inventoryCurrent) =>
        inventoryCurrent && package.Source == "winget" && package.Available is not null &&
        (upgrade ? package.CanUpgrade : package.Installed is null);

    public static bool IsPending(string status) =>
        status is OperationStatuses.Queued or OperationStatuses.Resolving or OperationStatuses.Running;
}

public static class OperationStatuses
{
    public const string Queued = "Queued";
    public const string Resolving = "Resolving";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string RebootRequired = "Reboot required";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string Unknown = "Unknown";
    public const string Skipped = "Skipped";
}
