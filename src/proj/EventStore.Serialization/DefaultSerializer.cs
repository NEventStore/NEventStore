namespace EventStore.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class DefaultSerializer : ISerializeObjects
	{
		private readonly IFormatter formatter = new BinaryFormatter();

		public virtual void Serialize(Stream output, object graph)
		{
			if (null != graph)
				this.formatter.Serialize(output, graph);
		}
		public virtual object Deserialize(Stream serialized)
		{
			return this.formatter.Deserialize(serialized);
		}
	}
}