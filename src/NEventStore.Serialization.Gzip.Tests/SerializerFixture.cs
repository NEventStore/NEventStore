// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using NEventStore.Serialization;

    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            _createSerializer = () =>
                new GzipSerializer(new BinarySerializer());
        }
    }
}