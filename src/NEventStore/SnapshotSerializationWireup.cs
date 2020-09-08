namespace NEventStore
{
    using NEventStore.Serialization;

    public class SnapshotSerializationWireup : Wireup
    {
        public SnapshotSerializationWireup(Wireup inner, ISerializeSnapshots serializer)
            : base(inner)
        {
            Container.Register(serializer);
        }
    }
}