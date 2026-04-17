using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests.BDD;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore
{
#if MSTEST
    [TestClass]
#endif
    public class when_creating_a_new_event_message
    {
        [Fact]
        public void should_expose_a_non_null_headers_dictionary()
        {
            var message = new EventMessage();

            message.Headers.Should().NotBeNull();
        }

        [Fact]
        public void should_allow_mutating_the_headers_dictionary()
        {
            var message = new EventMessage();

            message.Headers["key"] = "value";

            message.Headers["key"].Should().Be("value");
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
