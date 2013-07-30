// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
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