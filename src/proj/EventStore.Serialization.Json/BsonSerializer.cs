namespace NEventStore.Serialization
{
    using System;
    using System.Collections;
    using System.IO;
    using NEventStore.Logging;
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
			var writer = new BsonWriter(output) { DateTimeKindHandling = DateTimeKind.Utc };
			this.Serialize(writer, graph);
		}
		public override T Deserialize<T>(Stream input)
		{
			var reader = new BsonReader(input, IsArray(typeof(T)), DateTimeKind.Utc);
			return this.Deserialize<T>(reader);
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