#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using Machine.Specifications;

	[Subject("Serialization")]
	public class when_serializing_a_primitive_type : using_serialization
	{
		const long PrimitiveType = 1234;
		static byte[] serialized;

		Because of = () =>
			serialized = Serializer.Serialize(PrimitiveType);

		It should_deserialize_to_the_same_value = () =>
		{
			var value = Serializer.Deserialize(serialized);
			value.ShouldEqual(PrimitiveType);
		};
	}

	[Subject("Serialization")]
	public class when_serializing_a_simple_message : using_serialization
	{
		static readonly SimpleMessage Message = new SimpleMessage
		{
			Id = Guid.NewGuid(),
			Count = 1234,
			Created = DateTime.UtcNow,
			Value = "Hello, World",
			Contents = { "a", "b", "c" }
		};
		static byte[] serialized;

		Because of = () =>
			serialized = Serializer.Serialize(Message);

		It should_deserialize_to_the_same_value = () =>
		{
			var value = Serializer.Deserialize(serialized);
			value.Equals(Message).ShouldBeTrue();
		};
	}

	public abstract class using_serialization
	{
		protected static readonly ISerialize Serializer = new SerializationFactory().Build();
	}
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169