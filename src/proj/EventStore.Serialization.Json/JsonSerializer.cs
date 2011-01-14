namespace EventStore.Serialization.Json
{
	using System.IO;
	using Newtonsoft.Json;

	public class JsonSerializer : ISerialize
	{
		private readonly Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer
		{
			TypeNameHandling = TypeNameHandling.Objects
		};

		public void Serialize(Stream output, object graph)
		{
			var streamWriter = new StreamWriter(output);
			var jsonWriter = new JsonTextWriter(streamWriter);
			this.serializer.Serialize(jsonWriter, graph);
			jsonWriter.Flush();
			streamWriter.Flush();
		}
		public object Deserialize(Stream input)
		{
			using (var streamReader = new StreamReader(input))
			using (var jsonReader = new JsonTextReader(streamReader))
				return this.serializer.Deserialize(jsonReader);
		}
	}
}