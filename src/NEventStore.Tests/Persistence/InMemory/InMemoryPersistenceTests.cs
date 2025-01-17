﻿using NEventStore.Persistence.AcceptanceTests.BDD;
#pragma warning disable IDE1006 // Naming Styles

using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Persistence.InMemory
{
#if MSTEST
    [TestClass]
#endif
    public class when_getting_from_to_then_should_not_get_later_commits : SpecificationBase
    {
        private readonly DateTime _endDate = new(2013, 1, 2);
        private readonly DateTime _startDate = new(2013, 1, 1);
        private ICommit[]? _commits;
        private InMemoryPersistenceEngine? _engine;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();
            var streamId = Guid.NewGuid().ToString();
            _engine.Commit(new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, _startDate, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.Commit(new CommitAttempt(streamId, 2, Guid.NewGuid(), 2, _endDate, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFromTo(Bucket.Default, _startDate, _endDate).ToArray();
        }

        [Fact]
        public void should_return_two_commits()
        {
            _commits!.Length.Should().Be(1);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
