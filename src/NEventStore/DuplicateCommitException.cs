#region

using System;
using System.Runtime.Serialization;

#endregion

namespace NEventStore;

/// <summary>
///     Represents an attempt to commit the same information more than once.
/// </summary>
[Serializable]
public class DuplicateCommitException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the DuplicateCommitException class.
    /// </summary>
    public DuplicateCommitException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the DuplicateCommitException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DuplicateCommitException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the DuplicateCommitException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The message that is the cause of the current exception.</param>
    public DuplicateCommitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the DuplicateCommitException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data of the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected DuplicateCommitException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}