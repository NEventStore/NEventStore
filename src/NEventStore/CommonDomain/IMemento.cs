namespace CommonDomain
{
	using System;

	public interface IMemento
	{
		Guid Id { get; set; }

		int Version { get; set; }
	}
}