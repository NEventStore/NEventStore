namespace NEventStore
{
    /// <summary>
    ///   Represents an async observer that can receive notifications of commits.
    /// </summary>
    public class LambdaAsyncObserver<T> : IAsyncObserver<T>
    {
        private readonly Func<T, Task> _onNextAsync;
        private readonly Func<Exception, Task>? _onErrorAsync;
        private readonly Func<Task>? _onCompletedAsync;

        /// <summary>
        ///  Initializes a new instance of the LambdaAsyncObserver class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public LambdaAsyncObserver(
            Func<T, Task> onNextAsync,
            Func<Exception, Task>? onErrorAsync = null,
            Func<Task>? onCompletedAsync = null)
        {
            _onNextAsync = onNextAsync ?? throw new ArgumentNullException(nameof(onNextAsync));
            _onErrorAsync = onErrorAsync;
            _onCompletedAsync = onCompletedAsync;
        }

        /// <inheritdoc/>
        public Task OnCompletedAsync()
        {
            return _onCompletedAsync?.Invoke() ?? Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnErrorAsync(Exception ex)
        {
            return _onErrorAsync?.Invoke(ex) ?? Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnNextAsync(T value)
        {
            return _onNextAsync(value);
        }
    }
}
