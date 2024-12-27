using NEventStore.Serialization.Binary;

// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
#if NET8_0_OR_GREATER
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
#endif
#pragma warning disable CS0618 // Type or member is obsolete
            _createSerializer = () =>
                new BinarySerializer();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}