using FluentAssertions;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

#pragma warning disable IDE1006 // Naming Styles

namespace NEventStore.Async
{
#if MSTEST
    [TestClass]
#endif
    public class when_reusing_a_commit_identifier_after_reopening_a_stream : using_an_in_memory_store
    {
        private readonly Guid _duplicateCommitId = Guid.NewGuid();
        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            await base.ContextAsync().ConfigureAwait(false);
            await SeedCommitAsync(_duplicateCommitId, "already committed").ConfigureAwait(false);

            Stream = await Store.OpenStreamAsync(StreamId, 0, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
            Stream.Add(new EventMessage { Body = "duplicate attempt" });
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Stream.CommitChangesAsync(_duplicateCommitId, CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public void should_throw_a_DuplicateCommitException()
        {
            _thrown.Should().BeOfType<DuplicateCommitException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reusing_a_commit_identifier_from_history_that_was_not_loaded : using_an_in_memory_store
    {
        private readonly Guid _duplicateCommitId = Guid.NewGuid();
        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            await base.ContextAsync().ConfigureAwait(false);
            await SeedCommitAsync(_duplicateCommitId, "first event").ConfigureAwait(false);
            await SeedCommitAsync(Guid.NewGuid(), "second event").ConfigureAwait(false);

            // This stream is reopened from revision 2, so it sees the correct head revision and
            // commit sequence but does not materialize the first commit at all. The duplicate check
            // must therefore come from persistence, because a stream-local cache cannot observe the
            // earlier commit identifier in this shape of read.
            Stream = await Store.OpenStreamAsync(StreamId, 2, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
            Stream.Add(new EventMessage { Body = "duplicate attempt" });
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Stream.CommitChangesAsync(_duplicateCommitId, CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public void should_throw_a_DuplicateCommitException()
        {
            _thrown.Should().BeOfType<DuplicateCommitException>();
        }
    }

    public abstract class using_an_in_memory_store : SpecificationBase
    {
        private IStoreEvents? _store;
        private IEventStream? _stream;

        protected string StreamId { get; } = Guid.NewGuid().ToString();

        protected IStoreEvents Store => _store!;

        protected IEventStream Stream
        {
            get { return _stream!; }
            set { _stream = value; }
        }

        protected override Task ContextAsync()
        {
            // These duplicate-commit scenarios must go through a real persistence implementation.
            // The production change removes the stream-local CommitId cache precisely because it was
            // only a partial view of the stream history after reopening from arbitrary revisions.
            // Using the in-memory store here keeps the test focused on the public contract that
            // matters to callers: duplicate commit identifiers are rejected by the persistence
            // boundary even when the current stream instance did not load the conflicting commit.
            _store = Wireup.Init().UsingInMemoryPersistence().Build();
            return Task.CompletedTask;
        }

        protected override void Cleanup()
        {
            _stream?.Dispose();
            _store?.Dispose();
        }

        protected async Task SeedCommitAsync(Guid commitId, string eventBody)
        {
            using var stream = await Store.OpenStreamAsync(StreamId, 0, int.MaxValue, CancellationToken.None).ConfigureAwait(false);
            stream.Add(new EventMessage { Body = eventBody });
            await stream.CommitChangesAsync(commitId, CancellationToken.None).ConfigureAwait(false);
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
