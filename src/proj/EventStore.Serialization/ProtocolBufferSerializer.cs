namespace EventStore.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using System.Security.Cryptography;
	using System.Text;
	using Persistence;
	using ProtoBuf;

	public class ProtocolBufferSerializer : ISerialize
	{
		private const int GuidLengthInBytes = 16;
		private static readonly MD5 TypeHasher = MD5.Create(); // creates 16-byte hashes
		private readonly Dictionary<Guid, Type> hashes = new Dictionary<Guid, Type>();
		private readonly Dictionary<Type, Guid> types = new Dictionary<Type, Guid>();
		private readonly Dictionary<Type, Func<Stream, object>> deserializers =
			new Dictionary<Type, Func<Stream, object>>();

		public ProtocolBufferSerializer(params string[] contractAssemblyFileNamePatterns)
			: this(contractAssemblyFileNamePatterns.LoadAssemblies())
		{
		}
		public ProtocolBufferSerializer(params Assembly[] contractAssemblies)
			: this(contractAssemblies.SelectMany(assembly => assembly.GetTypes()).ToArray())
		{
		}
		public ProtocolBufferSerializer(params Type[] dataContracts)
			: this()
		{
			foreach (var contract in dataContracts ?? new Type[] { })
				this.RegisterContract(contract);
		}
		public ProtocolBufferSerializer()
		{
			this.RegisterPrimitives();
			this.RegisterCommonTypes();
		}

		private void RegisterPrimitives()
		{
			this.RegisterContract(typeof(bool));

			this.RegisterContract(typeof(char));
			this.RegisterContract(typeof(byte));
			this.RegisterContract(typeof(sbyte));

			this.RegisterContract(typeof(short));
			this.RegisterContract(typeof(ushort));
			this.RegisterContract(typeof(int));
			this.RegisterContract(typeof(uint));
			this.RegisterContract(typeof(long));
			this.RegisterContract(typeof(ulong));
			this.RegisterContract(typeof(double));
			this.RegisterContract(typeof(float));
			this.RegisterContract(typeof(decimal));

			this.RegisterContract(typeof(string));
		}
		private void RegisterCommonTypes()
		{
			this.RegisterContract(typeof(Commit));
			this.RegisterContract(typeof(EventMessage));
			this.RegisterContract(typeof(Dictionary<string, object>));
			this.RegisterContract(typeof(List<object>));
			this.RegisterContract(typeof(List<EventMessage>));
			this.RegisterContract(typeof(LinkedList<object>));
			this.RegisterContract(typeof(LinkedList<EventMessage>));

			this.RegisterContract(typeof(Uri));
			this.RegisterContract(typeof(Guid));
			this.RegisterContract(typeof(Exception));
			this.RegisterContract(typeof(SerializationException));
		}

		public void RegisterContract(Type contract)
		{
			if (!this.CanRegisterContract(contract))
				return;

			this.RegisterHash(contract);
			this.RegisterDeserializer(contract);
		}
		private bool CanRegisterContract(Type contract)
		{
			return contract != null
				&& !this.types.ContainsKey(contract)
				&& !string.IsNullOrEmpty(contract.FullName);
		}
		private void RegisterHash(Type contract)
		{
			var bytes = Encoding.Unicode.GetBytes(contract.FullName ?? string.Empty);
			var hash = new Guid(TypeHasher.ComputeHash(bytes));
			this.hashes[hash] = contract;
			this.types[contract] = hash;
		}
		private void RegisterDeserializer(Type contract)
		{
			// TODO: make this faster by using reflection to create a delegate and then invoking the delegate
			var deserialize = typeof(Serializer).GetMethod("Deserialize").MakeGenericMethod(contract);
			this.deserializers[contract] = stream => deserialize.Invoke(null, new object[] { stream });
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
			Guid hash;
			if (!this.types.TryGetValue(contract, out hash))
				throw new SerializationException(ExceptionMessages.UnableToSerialize.FormatWith(contract));

			var header = hash.ToByteArray();
			output.Write(header, 0, GuidLengthInBytes);
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
			var header = new byte[GuidLengthInBytes];
			serialized.Read(header, 0, header.Length);
			var hash = new Guid(header);

			Type contract;
			if (!this.hashes.TryGetValue(hash, out contract))
				throw new SerializationException(ExceptionMessages.UnableToDeserialize.FormatWith(hash));

			return contract;
		}
	}
}