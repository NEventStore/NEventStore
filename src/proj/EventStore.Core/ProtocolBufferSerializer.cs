namespace EventStore.Core
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using ProtoBuf;

	public class ProtocolBufferSerializer : ISerializeObjects
	{
		private readonly Dictionary<int, Type> hashes = new Dictionary<int, Type>();
		private readonly Dictionary<Type, int> types = new Dictionary<Type, int>();
		private readonly Dictionary<Type, Func<Stream, object>> deserializers =
			new Dictionary<Type, Func<Stream, object>>();

		public ProtocolBufferSerializer(params Assembly[] assemblies)
		{
			foreach (var assembly in assemblies)
				foreach (var type in assembly.GetTypes())
					this.RegisterType(type);
		}
		public ProtocolBufferSerializer(params Type[] types)
		{
			foreach (var type in types ?? new Type[] { })
				this.RegisterType(type);
		}
		private void RegisterType(Type type)
		{
			if (!this.CanRegisterType(type) || string.IsNullOrEmpty(type.FullName))
				return;

			var hash = type.FullName.GetHashCode();
			this.hashes[hash] = type;
			this.types[type] = hash;

			// TODO: make this faster by using reflection to create a delegate and then invoking the delegate
			var deserialize = typeof(Serializer).GetMethod("Deserialize").MakeGenericMethod(type);
			this.deserializers[type] = stream => deserialize.Invoke(null, new object[] { stream });
		}
		private bool CanRegisterType(Type type)
		{
			return null != type
			       && !this.types.ContainsKey(type)
			       && type.GetCustomAttributes(typeof(DataContractAttribute), false).Any();
		}

		public virtual void Serialize(Stream output, object graph)
		{
			if (null == graph)
				return;

			var type = graph.GetType();
			this.WriteTypeToStream(output, type);
			Serializer.Serialize(output, graph);
		}
		private void WriteTypeToStream(Stream output, Type type)
		{
			int hash;
			if (!this.types.TryGetValue(type, out hash))
				throw new SerializationException("Unable to serialize"); // TODO

			var header = BitConverter.GetBytes(hash);
			output.Write(header, 0, header.Length);
		}

		public virtual object Deserialize(Stream serialized)
		{
			var type = this.ReadType(serialized);
			return this.deserializers[type](serialized);
		}
		private Type ReadType(Stream serialized)
		{
			var header = new byte[4];
			serialized.Read(header, 0, header.Length);
			var hash = BitConverter.ToInt32(header, 0);

			Type type;
			if (!this.hashes.TryGetValue(hash, out type))
				throw new SerializationException("Unable to deserialize"); // TODO

			return type;
		}
	}
}