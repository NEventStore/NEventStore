using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EventStore
{
    /// <summary>
    /// Represents the failure that occurs when there are two or more event converters created for the same source type.
    /// </summary>
    [Serializable]
    public class MultipleConvertersFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        public MultipleConvertersFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        public MultipleConvertersFoundException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        public MultipleConvertersFoundException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MultipleConvertersFoundException class.
        /// </summary>
        protected MultipleConvertersFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
