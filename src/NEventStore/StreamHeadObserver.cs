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
        public virtual Task OnNextAsync(IStreamHead value)
        {
            StreamHeads.Add(value);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Throws an exception when an error occurs while reading commits from the stream
        /// </summary>
        /// <exception cref="AsyncObserverException"></exception>
        public virtual Task OnErrorAsync(Exception ex)
        {
            // todo: lo the error.
            // Preserve the stack trace and rethrow the exception
            ExceptionDispatchInfo.Capture(ex).Throw();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual Task OnCompletedAsync()
        {
            ReadCompleted = true;
            return Task.CompletedTask;
        }
    }
}