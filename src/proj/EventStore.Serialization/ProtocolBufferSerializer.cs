namespace EventStore.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using ProtoBuf;

	public class ProtocolBufferSerializer : ISerialize
	{
		private readonly Dictionary<int, Type> hashes = new Dictionary<int, Type>();
		private readonly Dictionary<Type, int> types = new Dictionary<Type, int>();
		private readonly Dictionary<Type, Func<Stream, object>> deserializers =
			new Dictionary<Type, Func<Stream, object>>();

		public ProtocolBufferSerializer(params string[] contractAssemblyFileNamePatterns)
			: this(contractAssemblyFileNamePatterns.LoadAssemblies())
		{
		}
		public ProtocolBufferSerializer(params Assembly[] contractAssemblies)
		{
			foreach (var type in contractAssemblies.SelectMany(assembly => assembly.GetTypes()))
				this.RegisterContract(type);
		}
		public ProtocolBufferSerializer(params Type[] dataContracts)
		{
			foreach (var contract in dataContracts ?? new Type[] { })
				this.RegisterContract(contract);
		}
		private void RegisterContract(Type contract)
		{
			if (!this.CanRegisterContract(contract) || string.IsNullOrEmpty(contract.FullName))
				return;

			// TODO: GetHashCode() is a *terrible* way to get contract "identity".
			var hash = contract.FullName.GetHashCode();
			this.hashes[hash] = contract;
			this.types[contract] = hash;

			// TODO: make this faster by using reflection to create a delegate and then invoking the delegate
			var deserialize = typeof(Serializer).GetMethod("Deserialize").MakeGenericMethod(contract);
			this.deserializers[contract] = stream => deserialize.Invoke(null, new object[] { stream });
		}
		private bool CanRegisterContract(Type contract)
		{
			return contract != null
				&& !this.types.ContainsKey(contract)
				&& contract.HasAttribute<DataContractAttribute>();
		}

		public virtual void Serialize(Stream output, object graph)
		{
			if (null == graph)
				return;

			var contract = graph.GetType();
			this.WriteContractTypeToStream(output, contract);
			Serializer.Serialize(output, graph);
		}
		private void WriteContractTypeToStream(Stream output, Type contract)
		{
			int hash;
			if (!this.types.TryGetValue(contract, out hash))
				throw new SerializationException(ExceptionMessages.UnableToSerialize.FormatWith(contract));

			var header = BitConverter.GetBytes(hash);
			output.Write(header, 0, header.Length);
		}

		public virtual object Deserialize(Stream input)
		{
			if (input == null)
				return null;

			var contractType = this.ReadContractType(input);
			return this.deserializers[contractType](input);
		}
		private Type ReadContractType(Stream serialized)
		{
			var header = new byte[4];
			serialized.Read(header, 0, header.Length);
			var hash = BitConverter.ToInt32(header, 0);

			Type contract;
			if (!this.hashes.TryGetValue(hash, out contract))
				throw new SerializationException(ExceptionMessages.UnableToDeserialize.FormatWith(hash));

			return contract;
		}
	}
}