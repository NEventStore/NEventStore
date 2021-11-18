﻿// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    using NEventStore.Serialization;

    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _createSerializer = () =>
                new BinarySerializer();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}