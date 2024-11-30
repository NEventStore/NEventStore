// ReSharper disable CheckNamespace

namespace NEventStore.Serialization.AcceptanceTests;
// ReSharper restore CheckNamespace

public partial class SerializerFixture
{
    private static readonly byte[] EncryptionKey = new byte[]
        { 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7, 0x8, 0x9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0x0 };

    public SerializerFixture()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        _createSerializer = () => new RijndaelSerializer(new BinarySerializer(), EncryptionKey);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}