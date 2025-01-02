namespace NEventStore
{
    /// <summary>
    /// Represents an async observer that can receive and stores commits from a stream.
    /// </summary>
    public class CommitStreamObserver : IAsyncObserver<ICommit>
    {
        /// <summary>
        /// The list of commits read from the stream
        /// </summary>
        public IList<ICommit> Commits { get; } = [];

        /// <summary>
        /// Indicates if the read operation has completed
        /// </summary>
        public bool ReadCompleted { get; private set; }

        /// <summary>
        /// Store the commits received from the stream
        /// </summary>
        public Task OnNextAsync(ICommit value)
        {
            Commits.Add(value);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Throws an exception when an error occurs while reading commits from the stream
        /// </summary>
        /// <exception cref="CommitStreamObserverException"></exception>
        public Task OnErrorAsync(long checkpoint, Exception ex)
        {
            throw new CommitStreamObserverException("Error reading commits.", ex) {
                Checkpoint = checkpoint
            };
        }

        /// <inheritdoc/>
        public Task OnCompletedAsync(long checkpoint)
        {
            ReadCompleted = true;
            return Task.CompletedTask;
        }
    }
}