namespace NEventStore.Persistence.AcceptanceTests
{
    [Serializable]
    public class SimpleMessage
    {
        public SimpleMessage()
        {
            Contents = [];
        }

        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public string? Value { get; set; }
        public int Count { get; set; }

        public List<string?> Contents { get; private set; }
    }
}