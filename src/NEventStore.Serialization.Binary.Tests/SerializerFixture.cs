namespace NEventStore.Serialization.AcceptanceTests
{
    using NEventStore.Serialization;

    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            createSerializer = () =>
                new BinarySerializer();
        }
    }
}