namespace NEventStore.Serialization.Json
{
    using System;
    using System.Collections;
    using System.IO;
    using Newtonsoft.Json.Bson;
    using NEventStore.Logging;
	using System.Reflection;

	public class BsonSerializer : JsonSerializer
    {
        private static readonly ILog Logger = LogFactory.BuildLogger(typeof (BsonSerializer));

        public BsonSerializer(params Type[] knownTypes) : base(knownTypes)
        {}

        public override void Serialize<T>(Stream output, T graph)
        {
            var writer = new BsonWriter(output) {DateTimeKindHandling = DateTimeKind.Utc};
            Serialize(writer, graph);
        }

        public override T Deserialize<T>(Stream input)
        {
            var reader = new BsonReader(input, IsArray(typeof (T)), DateTimeKind.Utc);
            return Deserialize<T>(reader);
        }

        private static bool IsArray(Type type)
        {
#if !NETSTANDARD1_6
			bool array = typeof (IEnumerable).IsAssignableFrom(type) && !typeof (IDictionary).IsAssignableFrom(type);
#else
			bool array = typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type) && !typeof(IDictionary).GetTypeInfo().IsAssignableFrom(type);
#endif

			Logger.Verbose(Messages.TypeIsArray, type, array);

            return array;
        }
    }
}