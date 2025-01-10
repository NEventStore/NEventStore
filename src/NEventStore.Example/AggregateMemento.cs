namespace NEventStore.Example
{
	internal class AggregateMemento
	{
		public string? Value { get; set; }

		public override string? ToString()
		{
			return Value;
		}
	}
}