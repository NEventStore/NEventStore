namespace NEventStore.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class TaskHelpers
    {
        internal static Task Delay(double milliseconds, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            var timer = new System.Timers.Timer();
            timer.Elapsed += (obj, args) => tcs.TrySetResult(true);
            timer.Interval = milliseconds;
            timer.AutoReset = false;
            timer.Start();
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                timer.Stop();
                tcs.TrySetCanceled();
            });
            return tcs.Task.ContinueWith(_ =>
            {
                cancellationTokenRegistration.Dispose();
                timer.Dispose();
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static void WhenCompleted<T>(this Task<T> task, Action<Task<T>> onComplete, Action<Task<T>> onFaulted, bool execSync = false)
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
                execSync ?
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion :
                    TaskContinuationOptions.OnlyOnRanToCompletion);

            task.ContinueWith(
                              onFaulted,
                execSync ?
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted :
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        public static void WhenCompleted(this Task task, Action<Task> onComplete, Action<Task> onFaulted, bool execSync = false)
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
                execSync ?
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion :
                    TaskContinuationOptions.OnlyOnRanToCompletion);

            task.ContinueWith(
                              onFaulted,
                execSync ?
                    TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted :
                    TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}