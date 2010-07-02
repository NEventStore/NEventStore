#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.SqlStorage.UnitTests
{
	using Machine.Specifications;

	[Subject("SqlStorageEngine")]
	public class when_
	{
		Establish context;
		Because of;
		It should;
	}
}

#pragma warning restore 169
// ReSharper enable InconsistentNaming