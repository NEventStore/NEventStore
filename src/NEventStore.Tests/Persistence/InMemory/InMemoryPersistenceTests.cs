using NEventStore.Persistence.AcceptanceTests.BDD;
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

#if MSTEST
    [TestClass]
#endif
    public class when_getting_from_to_checkpoint_then_should_not_get_later_commits : SpecificationBase
    {
        private readonly DateTime _endDate = new(2013, 1, 2);
        private readonly DateTime _startDate = new(2013, 1, 1);
        private ICommit[]? _commits;
        private InMemoryPersistenceEngine? _engine;
        private ICommit? _commit1;
        private ICommit? _commit2;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();
            var streamId = Guid.NewGuid().ToString();
            _commit1 = _engine.Commit(new CommitAttempt(streamId, 1, Guid.NewGuid(), 1, _startDate, new Dictionary<string, object>(), [new EventMessage()]));
            _commit2 = _engine.Commit(new CommitAttempt(streamId, 2, Guid.NewGuid(), 2, _endDate, new Dictionary<string, object>(), [new EventMessage()]));
            _engine.Commit(new CommitAttempt(streamId, 3, Guid.NewGuid(), 3, _endDate.AddDays(1), new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFromTo(Bucket.Default, _commit1!.CheckpointToken, _commit2!.CheckpointToken).ToArray();
        }

        [Fact]
        public void should_return_two_commits()
        {
            _commits!.Length.Should().Be(1);
            _commit2!.Should().Be(_commits![0]);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_bucket_commits_after_a_checkpoint_that_only_exists_in_another_bucket : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ICommit[]? _commits;
        private ICommit? _expected;
        private long _checkpointFromOtherBucket;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // This regression proves bucket checkpoint reads are based on the global checkpoint
            // position, not on whether the lower-bound checkpoint exists inside the same bucket.
            _engine.Commit(new CommitAttempt("b1", "stream-1", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
            _checkpointFromOtherBucket = _engine.Commit(new CommitAttempt("b2", "stream-2", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]))!.CheckpointToken;
            _expected = _engine.Commit(new CommitAttempt("b1", "stream-1", 2, Guid.NewGuid(), 2, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFrom("b1", _checkpointFromOtherBucket).ToArray();
        }

        [Fact]
        public void should_only_return_bucket_commits_after_the_checkpoint()
        {
            _commits!.Should().HaveCount(1);
            _commits[0].Should().Be(_expected);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_stream_commits_from_a_revision_range : SpecificationBase
    {
        private InMemoryPersistenceEngine? _engine;
        private ICommit[]? _commits;
        private ICommit? _first;
        private ICommit? _second;

        protected override void Context()
        {
            _engine = new InMemoryPersistenceEngine();
            _engine.Initialize();

            // The first commit ends before the requested minimum revision and should be skipped,
            // while the second overlaps the requested range and must still be returned.
            _first = _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 2, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage(), new EventMessage()]));
            _second = _engine.Commit(new CommitAttempt(Bucket.Default, "stream-1", 4, Guid.NewGuid(), 2, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage(), new EventMessage()]));
            _engine.Commit(new CommitAttempt(Bucket.Default, "stream-2", 1, Guid.NewGuid(), 1, DateTime.UtcNow, new Dictionary<string, object>(), [new EventMessage()]));
        }

        protected override void Because()
        {
            _commits = _engine!.GetFrom(Bucket.Default, "stream-1", 3, 4).ToArray();
        }

        [Fact]
        public void should_only_return_commits_that_overlap_the_requested_revision_range()
        {
            _commits!.Should().HaveCount(1);
            _commits[0].Should().Be(_second);
            _commits.Should().NotContain(_first);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
