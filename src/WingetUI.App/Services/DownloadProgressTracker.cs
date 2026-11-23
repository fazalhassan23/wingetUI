namespace WingetUI.App.Services;

public sealed record DownloadMetrics(
    ulong DownloadedBytes,
    ulong TotalBytes,
    double BytesPerSecond,
    TimeSpan? EstimatedRemaining);

public sealed class DownloadProgressTracker
{
    private const double SmoothingFactor = 0.35;
    private ulong previousBytes;
    private DateTimeOffset? previousObservation;
    private double smoothedBytesPerSecond;

    public DownloadMetrics Update(ulong downloadedBytes, ulong totalBytes, DateTimeOffset observedAt)
    {
        if (previousObservation is not null && downloadedBytes >= previousBytes)
        {
            var elapsedSeconds = (observedAt - previousObservation.Value).TotalSeconds;
            if (elapsedSeconds >= 0.2)
            {
                var instantaneousRate = (downloadedBytes - previousBytes) / elapsedSeconds;
                smoothedBytesPerSecond = smoothedBytesPerSecond == 0
                    ? instantaneousRate
                    : (SmoothingFactor * instantaneousRate) +
                      ((1 - SmoothingFactor) * smoothedBytesPerSecond);
            }
        }
        else if (downloadedBytes < previousBytes)
        {
            smoothedBytesPerSecond = 0;
        }

        previousBytes = downloadedBytes;
        previousObservation = observedAt;
        TimeSpan? remaining = totalBytes > downloadedBytes && smoothedBytesPerSecond > 0
            ? TimeSpan.FromSeconds((totalBytes - downloadedBytes) / smoothedBytesPerSecond)
            : null;
        return new(downloadedBytes, totalBytes, smoothedBytesPerSecond, remaining);
    }
}
