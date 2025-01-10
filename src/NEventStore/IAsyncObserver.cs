namespace NEventStore
{
    /// <summary>
    ///    Represents an async observer that can receive notifications of commits.
    /// </summary>
    public interface IAsyncObserver<T>
    {
        /// <summary>
        ///    Provides the observer with new data.
        /// </summary>
        Task OnNextAsync(T value, CancellationToken cancellationToken);
        /// <summary>
        ///   Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="ex">The error that has occurred.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        Task OnErrorAsync(Exception ex, CancellationToken cancellationToken);
        /// <summary>
        ///   Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        Task OnCompletedAsync(CancellationToken cancellationToken);
    }
}