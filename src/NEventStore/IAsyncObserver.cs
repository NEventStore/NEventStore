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
        Task OnNextAsync(T value);
        /// <summary>
        ///   Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="ex">The error that has occurred.</param>
        Task OnErrorAsync(Exception ex);
        /// <summary>
        ///   Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        Task OnCompletedAsync();
    }
}