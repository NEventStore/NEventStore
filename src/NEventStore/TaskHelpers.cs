#region

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace NEventStore;

internal static class TaskHelpers
{
    internal static Task Delay(double milliseconds, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        TimerCallback callback = _ => tcs.TrySetResult(true);
#pragma warning disable IDE0067 // Dispose objects before losing scope
        var timer = new Timer(callback, null, 0, Convert.ToInt32(milliseconds));
#pragma warning restore IDE0067 // Dispose objects before losing scope
        var cancellationTokenRegistration = cancellationToken.Register(() =>
        {
            timer.Dispose();
            tcs.TrySetCanceled();
        });
        return tcs.Task.ContinueWith(_ =>
        {
            cancellationTokenRegistration.Dispose();
            timer.Dispose();
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public static void WhenCompleted<T>(this Task<T> task, Action<Task<T>> onComplete, Action<Task<T>> onFaulted,
        bool execSync = false)
    {
        if (task.IsCompleted)
        {
            if (task.IsFaulted)
            {
                onFaulted.Invoke(task);
                return;
            }

            onComplete.Invoke(task);
            return;
        }

        task.ContinueWith(
            onComplete,
            execSync
                ? TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion
                : TaskContinuationOptions.OnlyOnRanToCompletion);

        task.ContinueWith(
            onFaulted,
            execSync
                ? TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted
                : TaskContinuationOptions.OnlyOnFaulted);
    }

    public static void WhenCompleted(this Task task, Action<Task> onComplete, Action<Task> onFaulted,
        bool execSync = false)
    {
        if (task.IsCompleted)
        {
            if (task.IsFaulted)
            {
                onFaulted.Invoke(task);
                return;
            }

            onComplete.Invoke(task);
            return;
        }

        task.ContinueWith(
            onComplete,
            execSync
                ? TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion
                : TaskContinuationOptions.OnlyOnRanToCompletion);

        task.ContinueWith(
            onFaulted,
            execSync
                ? TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted
                : TaskContinuationOptions.OnlyOnFaulted);
    }
}