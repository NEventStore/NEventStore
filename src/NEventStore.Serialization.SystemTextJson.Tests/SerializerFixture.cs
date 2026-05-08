// ReSharper disable CheckNamespace
using NEventStore.Serialization.SystemTextJson;

namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            _createSerializer = () =>
                new SystemTextJsonSerializer();
        }
    }
}
