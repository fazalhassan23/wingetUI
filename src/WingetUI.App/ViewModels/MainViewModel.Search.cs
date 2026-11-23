namespace WingetUI.App;

public partial class MainViewModel
{
    partial void OnQueryChanged(string value)
    {
        search?.Cancel();
        SearchResults = [];
        if (!string.IsNullOrWhiteSpace(value))
            _ = SearchAsync(value.Trim());
    }

    private async Task SearchAsync(string query)
    {
        using var request = new CancellationTokenSource();
        search = request;
        try
        {
            await Task.Delay(300, request.Token);
            Status = "Searching the WinGet community source…";
            var results = await backend.SearchAsync(query, request.Token);
            if (!request.IsCancellationRequested)
            {
                SearchResults = results;
                Status = results.Count == 0 ? "No matching packages." : $"{results.Count} matches.";
            }
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
            // A newer query superseded this search; only the latest request may update presentation state.
        }
        catch (Exception error)
        {
            if (!request.IsCancellationRequested)
                Status = $"WinGet search failed (0x{error.HResult:X8}). Check App Installer and try again.";
        }
        finally
        {
            if (search == request)
                search = null;
        }
    }
}
