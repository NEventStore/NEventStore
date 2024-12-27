namespace NEventStore.Serialization
{
    /// <summary>
    ///    A simple serializer that does not perform any serialization.
    /// </summary>
    public class DocumentObjectSerializer : IDocumentSerializer
    {
        /// <inheritdoc/>
        public object Serialize<T>(T graph) where T : notnull
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }
            return graph;
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(object document)
        {
            return (T?)document;
        }
    }
}