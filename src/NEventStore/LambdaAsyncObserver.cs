namespace NEventStore
{
    /// <summary>
    ///   Represents an async observer that can receive notifications of commits.
    /// </summary>
    public class LambdaAsyncObserver<T> : IAsyncObserver<T>
    {
        private readonly Func<T, CancellationToken, Task> _onNextAsync;
        private readonly Func<Exception, CancellationToken, Task>? _onErrorAsync;
        private readonly Func<CancellationToken, Task>? _onCompletedAsync;

        /// <summary>
        ///  Initializes a new instance of the LambdaAsyncObserver class.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public LambdaAsyncObserver(
            Func<T, CancellationToken, Task> onNextAsync,
            Func<Exception, CancellationToken, Task>? onErrorAsync = null,
            Func<CancellationToken, Task>? onCompletedAsync = null)
        {
            _onNextAsync = onNextAsync ?? throw new ArgumentNullException(nameof(onNextAsync));
            _onErrorAsync = onErrorAsync;
            _onCompletedAsync = onCompletedAsync;
        }

        /// <inheritdoc/>
        public Task OnCompletedAsync(CancellationToken cancellationToken)
        {
            return _onCompletedAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnErrorAsync(Exception ex, CancellationToken cancellationToken)
        {
            return _onErrorAsync?.Invoke(ex, cancellationToken) ?? Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task OnNextAsync(T value, CancellationToken cancellationToken)
        {
            return _onNextAsync(value, cancellationToken);
        }
    }
}
