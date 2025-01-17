// ReSharper disable CheckNamespace
using NEventStore.Serialization.SystemTextJson;
using System.Text.Json.Serialization.Metadata;

namespace NEventStore.Serialization.AcceptanceTests
// ReSharper restore CheckNamespace
{
    public partial class SerializerFixture
    {
        public SerializerFixture()
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        static typeInfo =>
                        {
                            // if the is a primitive type return

                            if (typeInfo.Type.IsPrimitive)
                            {
                                return;
                            }

                            // if it's Object return

                            if (typeInfo.Type == typeof(object))
                            {
                                return;
                            }


                            typeInfo.PolymorphismOptions = new()
                            {
                                TypeDiscriminatorPropertyName = "__type",
                                DerivedTypes =
                                {
                                    new JsonDerivedType(typeInfo.Type, typeInfo.Type.FullName)
                                }
                            };
                        }
                    }
                }
            };

            _createSerializer = () =>
                new SystemTextJsonSerializer(options);
        }
    }
}