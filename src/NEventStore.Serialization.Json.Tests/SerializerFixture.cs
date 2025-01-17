﻿using NEventStore.Serialization.Json;

// ReSharper disable CheckNamespace
namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            _createSerializer = () =>
                new JsonSerializer(null);
        }
    }
}