namespace EventStore.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using Persistence;

	public class XmlSerializer : ISerialize
	{
		private readonly DataContractSerializer serializer = new DataContractSerializer(typeof(Commit));

		public virtual void Serialize(Stream output, object graph)
		{
			if (null != graph)
				this.serializer.WriteObject(output, graph);
		}
		public virtual object Deserialize(Stream input)
		{
			return this.serializer.ReadObject(input);
		}
	}
}