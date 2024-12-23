namespace NEventStore.Helpers
{
    /// <summary>
    /// Provides a way to execute an action when disposed.
    /// </summary>
    internal sealed class DisposableAction : IDisposable
    {
        private Action? _disposeAction;

        public DisposableAction(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            // Interlocked allows the continuation to be executed only once
            var dispose = Interlocked.Exchange(ref _disposeAction, null);
            dispose?.Invoke();
        }
    }
}
