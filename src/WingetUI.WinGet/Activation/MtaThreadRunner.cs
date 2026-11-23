namespace WingetUI.WinGet.Activation;

internal static class MtaThreadRunner
{
    public static Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => Execute(work, cancellationToken, completion)) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        return completion.Task;
    }

    private static void Execute<T>(Func<T> work, CancellationToken cancellationToken, TaskCompletionSource<T> completion)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            completion.SetResult(work());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            completion.SetCanceled(cancellationToken);
        }
        catch (Exception error)
        {
            completion.SetException(error);
        }
    }
}
