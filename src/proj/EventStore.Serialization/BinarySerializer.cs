namespace EventStore.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;

	public class BinarySerializer : ISerialize
	{
		private readonly IFormatter formatter = new BinaryFormatter();

		public virtual void Serialize<T>(Stream output, T graph)
		{
			this.formatter.Serialize(output, graph);
		}
		public virtual T Deserialize<T>(Stream input)
		{
			return (T)this.formatter.Deserialize(input);
		}
	}
}