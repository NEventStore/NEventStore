// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using NEventStore.Serialization;
	
	public partial class SerializerFixture
    {
        public SerializerFixture()
        {
#if !NETSTANDARD1_6
			_createSerializer = () =>
                new GzipSerializer(new BinarySerializer());
#else
			_createSerializer = () =>
				new GzipSerializer(new NEventStore.Serialization.Json.JsonSerializer());
#endif
		}
	}
}