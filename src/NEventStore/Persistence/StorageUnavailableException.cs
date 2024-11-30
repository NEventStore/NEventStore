#region

using System;
using System.Runtime.Serialization;

#endregion

namespace NEventStore.Persistence;

/// <summary>
///     Indicates that the underlying persistence medium is unavailable or offline.
/// </summary>
[Serializable]
public class StorageUnavailableException : StorageException
{
    /// <summary>
    ///     Initializes a new instance of the StorageUnavailableException class.
    /// </summary>
    public StorageUnavailableException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the StorageUnavailableException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public StorageUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the StorageUnavailableException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The message that is the cause of the current exception.</param>
    public StorageUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the StorageUnavailableException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected StorageUnavailableException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}