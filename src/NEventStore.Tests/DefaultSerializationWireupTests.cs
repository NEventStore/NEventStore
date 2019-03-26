#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore
{
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using System;
    using FluentAssertions;
#if MSTEST
	using Microsoft.VisualStudio.TestTools.UnitTesting;	
#endif
#if NUNIT
    using NUnit.Framework;
    using NEventStore.Persistence.InMemory;
    using NEventStore.Tests;
    using NEventStore.Persistence;
#endif
#if XUNIT
	using Xunit;
	using Xunit.Should;
#endif

#if MSTEST
		[TestClass]
#endif

    public class when_building_an_event_store_without_an_explicit_serializer : SpecificationBase
    {
        private TestableWireup _wireup;
        private Exception _exception;
        private IStoreEvents _eventStore;

        protected override void Context()
        {
            _wireup = Wireup.Init()
                // .UsingInMemoryPersistence() // the InMemoryPersistence should be the default serializer
                .UseTestableWireup();
        }

        protected override void Because()
        {
            _exception = Catch.Exception(() => _eventStore = _wireup.Build());
        }

        protected override void Cleanup()
        {
            _eventStore.Dispose();
        }

        [Fact]
        public void should_not_throw_an_argument_null_exception()
        {
            // _exception.Should().NotBeOfType<ArgumentNullException>();
            _exception.Should().BeNull();
        }

        [Fact]
        public void should_have_InMemoryPersistenceEngine_as_default_serializer()
        {
            var defaultPersistence = _wireup.Container.Resolve<IPersistStreams>();
            defaultPersistence.Should().BeOfType(typeof(InMemoryPersistenceEngine));

            // cannot check eventstore.Advance type because the persistence is wrapped
            // by a PipelineHooksPersistenceAwareDecorator
            // _eventStore.Advanced.Should().BeOfType(typeof(InMemoryPersistenceEngine));
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
