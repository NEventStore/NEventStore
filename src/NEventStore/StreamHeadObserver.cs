using NEventStore.Persistence;
using System.Runtime.ExceptionServices;

namespace NEventStore
{
    /// <summary>
    /// Represents an async observer that can receive and stores commits from a stream.
    /// Can be used as base class for other observers.
    /// </summary>
    public class StreamHeadObserver : IAsyncObserver<IStreamHead>
    {
        /// <summary>
        /// The list of commits read from the stream
        /// </summary>
        public IList<IStreamHead> StreamHeads { get; } = [];

        /// <summary>
        /// Indicates if the read operation has completed
        /// </summary>
        public bool ReadCompleted { get; private set; }

        /// <summary>
        /// Store the commits received from the stream
        /// </summary>
        public virtual Task<bool> OnNextAsync(IStreamHead value, CancellationToken cancellationToken)
        {
            StreamHeads.Add(value);
            return Task.FromResult(true);
        }

        /// <summary>
        /// <para>Notifies the observer that the provider has experienced an error condition.</para>
        /// <para>
        /// Preserve the stack trace and rethrow the exception that occurred while reading commits from the stream.
        /// </para>
        /// <para>
        /// Override this method to log and handle the error.
        /// </para>
        /// </summary>
        public virtual Task OnErrorAsync(Exception ex, CancellationToken cancellationToken)
        {
            // Preserve the stack trace and rethrow the exception
            ExceptionDispatchInfo.Capture(ex).Throw();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnCompletedAsync(CancellationToken cancellationToken)
        {
            ReadCompleted = true;
            return Task.CompletedTask;
        }
    }
}