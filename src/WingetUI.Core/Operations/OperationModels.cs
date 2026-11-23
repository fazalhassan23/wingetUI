namespace WingetUI.Core.Operations;

public sealed record OperationProgress(string Phase, double? Percentage);
public sealed record OperationResult(string Status, string Detail);
public sealed class PackageBackendUnavailableException(string message) : Exception(message);
