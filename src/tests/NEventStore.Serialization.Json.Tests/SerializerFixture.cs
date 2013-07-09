namespace NEventStore.Serialization.AcceptanceTests
{
    using EventStore.Serialization;

    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            createSerializer = () =>
                new JsonSerializer();
        }
    }
}