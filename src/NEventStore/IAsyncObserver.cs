namespace NEventStore
{
    /// <summary>
    /// Represents an async observer that can receive notifications of objects.
    /// </summary>
    public interface IAsyncObserver<T>
    {
        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="value">The current notification information.</param>
        /// <param name="cancellationToken">the cancellation token of the original request.</param>
        /// <returns>
        /// true = continue reading, false = stop reading
        /// </returns>
        Task<bool> OnNextAsync(T value, CancellationToken cancellationToken);
        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="ex">The error that has occurred.</param>
        /// <param name="cancellationToken">the cancellation token of the original request.</param>
        Task OnErrorAsync(Exception ex, CancellationToken cancellationToken);
        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// It can happen because:
        /// - the source has completed sending push-based notifications successfully.
        /// - the reading operation has been cancelled.
        /// </summary>
        /// <param name="cancellationToken">the cancellation token of the original request.</param>
        Task OnCompletedAsync(CancellationToken cancellationToken);
    }
}