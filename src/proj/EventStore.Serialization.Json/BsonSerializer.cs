namespace EventStore.Serialization
{
	using System;
	using System.Collections;
	using System.IO;
	using Logging;
	using Newtonsoft.Json.Bson;

	public class BsonSerializer : JsonSerializer
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(BsonSerializer));

		public BsonSerializer(params Type[] knownTypes)
			: base(knownTypes)
		{
		}

		public override void Serialize<T>(Stream output, T graph)
		{
			this.Serialize(new BsonWriter(output), graph);
		}
		public override T Deserialize<T>(Stream input)
		{
			return this.Deserialize<T>(new BsonReader(
				input, IsArray(typeof(T)), DateTimeKind.Unspecified));
		}
		private static bool IsArray(Type type)
		{
			var array = typeof(IEnumerable).IsAssignableFrom(type)
				&& !typeof(IDictionary).IsAssignableFrom(type);

			Logger.Verbose(Messages.TypeIsArray, type, array);

			return array;
		}
	}
}