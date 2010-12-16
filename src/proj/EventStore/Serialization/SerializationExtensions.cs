namespace EventStore.Serialization
{
	using System.IO;

	/// <summary>
	/// Implements extension methods that make call to the serialization infrastructure more simple.
	/// </summary>
	public static class SerializationExtensions
	{
		/// <summary>
		/// Serializes the object provided.
		/// </summary>
		/// <param name="serializer">The serializer to use.</param>
		/// <param name="value">The object graph to be serialized.</param>
		/// <returns>A serialized representation of the object graph provided.</returns>
		public static byte[] Serialize(this ISerialize serializer, object value)
		{
			if (value == null)
				return new byte[] { };

			using (var stream = new MemoryStream())
			{
				serializer.Serialize(stream, value);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Deserializes the array of bytes provided.
		/// </summary>
		/// <param name="serializer">The serializer to use.</param>
		/// <param name="serialized">The serialized array of bytes.</param>
		/// <returns>The reconstituted object, if any.</returns>
		public static object Deserialize(this ISerialize serializer, byte[] serialized)
		{
			serialized = serialized ?? new byte[] { };
			if (serialized.Length == 0)
				return null;

			using (var stream = new MemoryStream(serialized))
				return serializer.Deserialize(stream);
		}
	}
}