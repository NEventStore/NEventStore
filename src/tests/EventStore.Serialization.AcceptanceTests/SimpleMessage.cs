namespace EventStore.Serialization.AcceptanceTests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[Serializable]
	public class SimpleMessage
	{
		public SimpleMessage()
		{
			this.Contents = new List<string>();
		}

		public Guid Id { get; set; }
		public DateTime Created { get; set; }
		public string Value { get; set; }
		public int Count { get; set; }
		public List<string> Contents { get; private set; }

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != typeof(SimpleMessage))
				return false;
			return this.Equals((SimpleMessage)obj);
		}
		public bool Equals(SimpleMessage other)
		{
			if (ReferenceEquals(null, other))
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return other.Id.Equals(this.Id)
			       && other.Created.Equals(this.Created)
			       && Equals(other.Value, this.Value)
			       && other.Count == this.Count
			       && other.Contents.SequenceEqual(this.Contents);
		}
		public override int GetHashCode()
		{
			unchecked
			{
				var result = this.Id.GetHashCode();
				result = (result * 397) ^ this.Created.GetHashCode();
				result = (result * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
				result = (result * 397) ^ this.Count;
				result = (result * 397) ^ (this.Contents != null ? this.Contents.GetHashCode() : 0);
				return result;
			}
		}
	}
}