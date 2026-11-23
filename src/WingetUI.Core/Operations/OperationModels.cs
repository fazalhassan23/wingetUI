namespace WingetUI.Core.Operations;

public sealed record OperationProgress(
    string Phase,
    double? Percentage,
    ulong? BytesDownloaded = null,
    ulong? BytesRequired = null);
public sealed record OperationResult(string Status, string Detail);
public sealed class PackageBackendUnavailableException(string message) : Exception(message);
