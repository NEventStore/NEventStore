using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests.BDD.NUnit;
using NEventStore.Persistence.InMemory;

namespace NEventStore.Tests.Persistence.InMemory;
#if MSTEST
	using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
	using Xunit;
	using Xunit.Should;
#endif

#if MSTEST
	[TestClass]
#endif
public class when_getting_from_to_then_should_not_get_later_commits : SpecificationBase
{
    private readonly DateTime _endDate = new(2013, 1, 2);
    private readonly DateTime _startDate = new(2013, 1, 1);
    private ICommit[] _commits;
    private InMemoryPersistenceEngine _engine;

    protected override void Context()
    {
        _engine = new InMemoryPersistenceEngine();
        _engine.Initialize();
        var streamId = Guid.NewGuid().ToString();
        _engine.Commit(new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, _startDate, new Dictionary<string, object>(),
            new[] { new EventMessage() }));
        _engine.Commit(new CommitAttempt(streamId, 2, Guid.NewGuid(), 2, _endDate, new Dictionary<string, object>(),
            new[] { new EventMessage() }));
    }

    protected override void Because()
    {
        _commits = _engine.GetFromTo(Bucket.Default, _startDate, _endDate).ToArray();
    }

    [Fact]
    public void should_return_two_commits()
    {
        _commits.Length.Should().Be(1);
    }
}