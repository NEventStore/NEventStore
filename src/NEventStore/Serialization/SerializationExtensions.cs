
namespace NEventStore.Serialization
{
    /// <summary>
    ///     Implements extension methods that make call to the serialization infrastructure more simple.
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        ///     Serializes the object provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be serialized</typeparam>
        /// <param name="serializer">The serializer to use.</param>
        /// <param name="value">The object graph to be serialized.</param>
        /// <returns>A serialized representation of the object graph provided.</returns>
        public static byte[] Serialize<T>(this ISerialize serializer, T value)
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, value);
            return stream.ToArray();
        }

        /// <summary>
        ///     Deserializes the array of bytes provided.
        /// </summary>
        /// <typeparam name="T">The type of object to be deserialized.</typeparam>
        /// <param name="serializer">The serializer to use.</param>
        /// <param name="serialized">The serialized array of bytes.</param>
        /// <returns>The reconstituted object, if any.</returns>
        public static T Deserialize<T>(this ISerialize serializer, byte[] serialized)
        {
            // add null or empty check
            if (serialized == null || serialized.Length == 0)
            {
                throw new ArgumentNullException(nameof(serialized), "cannot be null or empty.");
            }

            using var stream = new MemoryStream(serialized);
            return serializer.Deserialize<T>(stream);
        }
    }
}