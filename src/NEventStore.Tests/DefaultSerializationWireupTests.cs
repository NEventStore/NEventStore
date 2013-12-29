namespace NEventStore
{
	using NEventStore.Persistence.AcceptanceTests;
	using NEventStore.Persistence.AcceptanceTests.BDD;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Xunit;
	using Xunit.Should;

	public class DefaultSerializationWireupTests
	{
		public class when_building_an_event_store_without_an_explicit_serializer : SpecificationBase
		{
			private Wireup wireup;
			private Exception exception;
			protected override void Context()
			{
				wireup = Wireup.Init()
					.UsingSqlPersistence("fakeConnectionString")
						.WithDialect(new Persistence.Sql.SqlDialects.MsSqlDialect());
			}

			protected override void Because()
			{
				exception = Catch.Exception(() => { wireup.Build(); });
			}

			protected override void Cleanup()
			{
			}

			[Fact]
			public void should_not_throw_an_argument_null_exception()
			{
				exception.ShouldNotBeInstanceOf<ArgumentNullException>();
			}
		}
	}
}
