namespace NEventStore.Serialization.AcceptanceTests
{
    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            createSerializer = () =>
                new JsonSerializer();
        }
    }
}