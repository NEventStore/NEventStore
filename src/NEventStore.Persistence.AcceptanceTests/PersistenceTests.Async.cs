#pragma warning disable 169 // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

using NEventStore.Persistence.AcceptanceTests.BDD;
using FluentAssertions;
#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
using NUnit.Framework;
#endif
#if XUNIT
using Xunit;
using Xunit.Should;
#endif

namespace NEventStore.Persistence.AcceptanceTests.Async
{
#if MSTEST
    [TestClass]
#endif
    public class when_a_commit_header_has_a_name_that_contains_a_period : PersistenceEngineConcernAsync
    {
        private ICommit? _persisted;
        private string? _streamId;

        protected override Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            var attempt = new CommitAttempt(_streamId,
                2,
                Guid.NewGuid(),
                1,
                DateTime.UtcNow,
                new Dictionary<string, object> { { "key.1", "value" } },
                [new EventMessage { Body = new ExtensionMethods.SomeDomainEvent { SomeProperty = "Test" } }]);
            return Persistence.CommitAsync(attempt, CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_streamId!, 0, int.MaxValue, observer, CancellationToken.None);
            _persisted = observer.Commits[0];
        }

        [Fact]
        public void should_correctly_deserialize_headers()
        {
            _persisted.Should().NotBeNull();
            _persisted!.Headers.Keys.Should().Contain("key.1");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_commit_is_successfully_persisted : PersistenceEngineConcernAsync
    {
        private CommitAttempt? _attempt;
        private DateTime _now;
        private ICommit? _persisted;
        private string? _streamId;

        protected override Task ContextAsync()
        {
            _now = SystemTime.UtcNow.AddYears(1);
            _streamId = Guid.NewGuid().ToString();
            _attempt = _streamId.BuildAttempt(_now);

            return Persistence.CommitAsync(_attempt, CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_streamId!, 0, int.MaxValue, observer, CancellationToken.None);
            _persisted = observer.Commits[0];
        }

        [Fact]
        public void should_correctly_persist_the_stream_identifier()
        {
            _persisted!.StreamId.Should().Be(_attempt!.StreamId);
        }

        [Fact]
        public void should_correctly_persist_the_stream_stream_revision()
        {
            _persisted!.StreamRevision.Should().Be(_attempt!.StreamRevision);
        }

        [Fact]
        public void should_correctly_persist_the_commit_identifier()
        {
            _persisted!.CommitId.Should().Be(_attempt!.CommitId);
        }

        [Fact]
        public void should_correctly_persist_the_commit_sequence()
        {
            _persisted!.CommitSequence.Should().Be(_attempt!.CommitSequence);
        }

        // persistence engines have varying levels of precision with respect to time.
        [Fact]
        public void should_correctly_persist_the_commit_stamp()
        {
            var difference = _persisted!.CommitStamp.Subtract(_now);
            difference.Days.Should().Be(0);
            difference.Hours.Should().Be(0);
            difference.Minutes.Should().Be(0);
            difference.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void should_correctly_persist_the_headers()
        {
            _persisted!.Headers.Count.Should().Be(_attempt!.Headers.Count);
        }

        [Fact]
        public void should_correctly_persist_the_events()
        {
            _persisted!.Events.Count.Should().Be(_attempt!.Events.Count);
        }

        [Fact]
        public async Task should_cause_the_stream_to_be_found_in_the_list_of_streams_to_snapshot()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(1, observer, CancellationToken.None);
            observer.StreamHeads
                .FirstOrDefault(x => x.StreamId == _streamId).Should().NotBeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_from_a_given_revision : PersistenceEngineConcernAsync
    {
        private const int LoadFromCommitContainingRevision = 3;
        private const int UpToCommitWithContainingRevision = 5;
        private ICommit[]? _committed;
        private ICommit? _oldest, _oldest2, _oldest3;
        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _oldest = await Persistence.CommitSingleAsync(); // 2 events, revision 1-2
            _oldest2 = await Persistence.CommitNextAsync(_oldest!); // 2 events, revision 3-4
            _oldest3 = await Persistence.CommitNextAsync(_oldest2!); // 2 events, revision 5-6
            await Persistence.CommitNextAsync(_oldest3!); // 2 events, revision 7-8

            _streamId = _oldest!.StreamId;
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_streamId!, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision, observer, CancellationToken.None);
            _committed = observer.Commits.ToArray();
        }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
        {
            _committed![0].CommitId.Should().Be(_oldest2!.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed!.Last().CommitId.Should().Be(_oldest3!.CommitId); // contains revision 5
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_from_a_given_revision_to_commit_revision : PersistenceEngineConcernAsync
    {
        private const int LoadFromCommitContainingRevision = 3;
        private const int UpToCommitWithContainingRevision = 6;
        private ICommit[]? _committed;
        private ICommit? _oldest, _oldest2, _oldest3;
        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _oldest = await Persistence.CommitSingleAsync(); // 2 events, revision 1-2
            _oldest2 = await Persistence.CommitNextAsync(_oldest!); // 2 events, revision 3-4
            _oldest3 = await Persistence.CommitNextAsync(_oldest2!); // 2 events, revision 5-6
            await Persistence.CommitNextAsync(_oldest3!); // 2 events, revision 7-8

            _streamId = _oldest!.StreamId;
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_streamId!, LoadFromCommitContainingRevision, UpToCommitWithContainingRevision, observer, CancellationToken.None);
            _committed = observer.Commits.ToArray();
        }

        [Fact]
        public void should_start_from_the_commit_which_contains_the_min_stream_revision_specified()
        {
            _committed![0].CommitId.Should().Be(_oldest2!.CommitId); // contains revision 3
        }

        [Fact]
        public void should_read_up_to_the_commit_which_contains_the_max_stream_revision_specified()
        {
            _committed!.Last().CommitId.Should().Be(_oldest3!.CommitId); // contains revision 6
        }
    }

    public class when_observer_stops_reading_the_stream_after_2_commits : PersistenceEngineConcernAsync
    {
        private ICommit? _oldest, _oldest2, _oldest3;
        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _oldest = await Persistence.CommitSingleAsync(); // 2 events, revision 1-2
            _oldest2 = await Persistence.CommitNextAsync(_oldest!); // 2 events, revision 3-4
            _oldest3 = await Persistence.CommitNextAsync(_oldest2!); // 2 events, revision 5-6
            await Persistence.CommitNextAsync(_oldest3!); // 2 events, revision 7-8

            _streamId = _oldest!.StreamId;
        }

        [Fact]
        public async Task ICommitEvents_GetFromAsync_stops_after_2_commits()
        {
            bool _observerCompleted = false;
            var _committed = new List<ICommit>();
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (c, _) =>
                {
                    if (_committed.Count <= 1)
                    {
                        _committed.Add(c);
                        return Task.FromResult(true);
                    }
                    // do not read more than 2 commits
                    return Task.FromResult(false);
                },
                onCompletedAsync: (_) =>
                {
                    _observerCompleted = true;
                    return Task.CompletedTask;
                });
            await Persistence.GetFromAsync(Bucket.Default, _streamId!, 0, int.MaxValue, observer, CancellationToken.None);

            _committed!.Count.Should().Be(2);
            _observerCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task IPersistStreams_GetFromAsync_Checkpoint_stops_after_2_commits()
        {
            bool _observerCompleted = false;
            var _committed = new List<ICommit>();
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (c, _) =>
                {
                    if (_committed.Count <= 1)
                    {
                        _committed.Add(c);
                        return Task.FromResult(true);
                    }
                    // do not read more than 2 commits
                    return Task.FromResult(false);
                },
                onCompletedAsync: (_) =>
                {
                    _observerCompleted = true;
                    return Task.CompletedTask;
                });
            await Persistence.GetFromAsync(0, observer, CancellationToken.None);

            _committed!.Count.Should().Be(2);
            _observerCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task IPersistStreams_GetFromToAsync_Checkpoint_stops_after_2_commits()
        {
            bool _observerCompleted = false;
            var _committed = new List<ICommit>();
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (c, _) =>
                {
                    if (_committed.Count <= 1)
                    {
                        _committed.Add(c);
                        return Task.FromResult(true);
                    }
                    // do not read more than 2 commits
                    return Task.FromResult(false);
                },
                onCompletedAsync: (_) =>
                {
                    _observerCompleted = true;
                    return Task.CompletedTask;
                });
            await Persistence.GetFromToAsync(0, long.MaxValue, observer, CancellationToken.None);

            _committed!.Count.Should().Be(2);
            _observerCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task IPersistStreams_GetFromAsync_Bucket_Checkpoint_stops_after_2_commits()
        {
            bool _observerCompleted = false;
            var _committed = new List<ICommit>();
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (c, _) =>
                {
                    if (_committed.Count <= 1)
                    {
                        _committed.Add(c);
                        return Task.FromResult(true);
                    }
                    // do not read more than 2 commits
                    return Task.FromResult(false);
                },
                onCompletedAsync: (_) =>
                {
                    _observerCompleted = true;
                    return Task.CompletedTask;
                });
            await Persistence.GetFromAsync(Bucket.Default, 0, observer, CancellationToken.None);

            _committed!.Count.Should().Be(2);
            _observerCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task IPersistStreams_GetFromToAsync_Bucket_Checkpoint_stops_after_2_commits()
        {
            bool _observerCompleted = false;
            var _committed = new List<ICommit>();
            var observer = new LambdaAsyncObserver<ICommit>(
                onNextAsync: (c, _) =>
                {
                    if (_committed.Count <= 1)
                    {
                        _committed.Add(c);
                        return Task.FromResult(true);
                    }
                    // do not read more than 2 commits
                    return Task.FromResult(false);
                },
                onCompletedAsync: (_) =>
                {
                    _observerCompleted = true;
                    return Task.CompletedTask;
                });
            await Persistence.GetFromToAsync(Bucket.Default, 0, long.MaxValue, observer, CancellationToken.None);

            _committed!.Count.Should().Be(2);
            _observerCompleted.Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_revision : PersistenceEngineConcernAsync
    {
        private CommitAttempt? _attemptWithSameRevision;
        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            var commit = await Persistence.CommitSingleAsync();
            _attemptWithSameRevision = commit!.StreamId.BuildAttempt();
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_attemptWithSameRevision!, CancellationToken.None));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

    // This test ensure the uniqueness of BucketId+StreamId+CommitSequence 
    // to avoid concurrency issues
#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_sequence : PersistenceEngineConcernAsync
    {
        private CommitAttempt? _attempt1, _attempt2;
        private Exception? _thrown;

        protected override Task ContextAsync()
        {
            string streamId = Guid.NewGuid().ToString();
            _attempt1 = streamId.BuildAttempt();
            _attempt2 = new CommitAttempt(
                _attempt1.BucketId,         // <--- Same bucket
                _attempt1.StreamId,         // <--- Same stream it
                _attempt1.StreamRevision + 10,
                Guid.NewGuid(),
                _attempt1.CommitSequence,   // <--- Same commit seq
                DateTime.UtcNow,
                _attempt1.Headers,
                [
                    new EventMessage(){ Body = new ExtensionMethods.SomeDomainEvent {SomeProperty = "Test 3"}}
                ]
            );

            return Persistence.CommitAsync(_attempt1, CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_attempt2!, CancellationToken.None));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

    //TODO:This test looks exactly like the one above. What are we trying to prove?
#if MSTEST
    [TestClass]
#endif
    public class when_attempting_to_overwrite_a_committed_sequence : PersistenceEngineConcernAsync
    {
        private CommitAttempt? _failedAttempt;
        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            string streamId = Guid.NewGuid().ToString();
            CommitAttempt successfulAttempt = streamId.BuildAttempt();
            await Persistence.CommitAsync(successfulAttempt, CancellationToken.None);
            _failedAttempt = streamId.BuildAttempt();
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_failedAttempt!, CancellationToken.None));
        }

        [Fact]
        public void should_throw_a_ConcurrencyException()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_attempting_to_persist_a_commit_twice : PersistenceEngineConcernAsync
    {
        private CommitAttempt? _attemptTwice;
        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            var commit = await Persistence.CommitSingleAsync();
            _attemptTwice = new CommitAttempt(
                commit!.BucketId,
                commit.StreamId,
                commit.StreamRevision,
                commit.CommitId,
                commit.CommitSequence,
                commit.CommitStamp,
                commit.Headers.ToDictionary(k => k.Key, v => v.Value),
                commit.Events.ToArray());
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_attemptTwice!, CancellationToken.None));
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
    public class when_attempting_to_persist_a_commitId_twice_on_same_stream : PersistenceEngineConcernAsync
    {
        private CommitAttempt? _attemptTwice;
        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            var commit = await Persistence.CommitSingleAsync();
            _attemptTwice = new CommitAttempt(
                commit!.BucketId,
                commit.StreamId,
                commit.StreamRevision + 1,
                commit.CommitId,
                commit.CommitSequence + 1,
                commit.CommitStamp,
                commit.Headers.ToDictionary(k => k.Key, v => v.Value),
                commit.Events.ToArray()
            );
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_attemptTwice!, CancellationToken.None));
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
    public class when_committing_more_events_than_the_configured_page_size : PersistenceEngineConcernAsync
    {
        private CommitAttempt[]? _committed;
        private ICommit[]? _loaded;
        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            _committed = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 2, _streamId)).ToArray();
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_streamId!, 0, int.MaxValue, observer, CancellationToken.None);
            _loaded = observer.Commits.ToArray();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded!.Length.Should().Be(_committed!.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed!
                .All(commit => _loaded!.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_saving_a_snapshot : PersistenceEngineConcernAsync
    {
        private bool _added;
        private Snapshot? _snapshot;
        private string? _streamId;

        protected override Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_streamId, 1, "Snapshot");
            return Persistence.CommitSingleAsync(_streamId);
        }

        protected override async Task BecauseAsync()
        {
            _added = await Persistence.AddSnapshotAsync(_snapshot!, CancellationToken.None);
        }

        [Fact]
        public void should_indicate_the_snapshot_was_added()
        {
            _added.Should().BeTrue();
        }

        [Fact]
        public async Task should_be_able_to_retrieve_the_snapshot()
        {
            (await Persistence.GetSnapshotAsync(_streamId!, _snapshot!.StreamRevision, CancellationToken.None)).Should().NotBeNull();
        }
    }

    /// <summary>
    /// having multiple snapshots for the same tuple: bucketId, streamId, streamRevision
    /// should not be allowed, the resulting behavior should be ignoring or updating the
    /// snapshot, that was the original design (it's up to the driver decide what to do)
    /// this behavior can be changed in a future implementation.
    /// </summary>
#if MSTEST
    [TestClass]
#endif
    public class when_adding_multiple_snapshots_for_same_bucketId_streamId_streamRevision : PersistenceEngineConcernAsync
    {
        private bool _added;
        private Snapshot? _snapshot;
        private Snapshot? _updatedSnapshot;
        private string? _streamId;

        private Exception? _thrown;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_streamId, 1, "Snapshot");
            await Persistence.CommitSingleAsync(_streamId);

            await Persistence.AddSnapshotAsync(_snapshot, CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            _updatedSnapshot = new Snapshot(_streamId!, 1, "Updated Snapshot");
            _thrown = await Catch.ExceptionAsync(async () => _added = await Persistence.AddSnapshotAsync(_updatedSnapshot, CancellationToken.None));
        }

        [Fact]
        public void should_not_raise_exception()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public async Task should_be_able_to_retrieve_the_correct_snapshot_original_or_updated_depends_on_driver_implementation()
        {
            var snapshot = await Persistence.GetSnapshotAsync(_streamId!, _snapshot!.StreamRevision, CancellationToken.None);
            snapshot.Should().NotBeNull();
            if (_added)
            {
                snapshot!.Payload.Should().Be(_updatedSnapshot!.Payload, "The snapshot was added, I expected to get the most updated version");
            }
            else
            {
                snapshot!.Payload.Should().Be(_snapshot.Payload, "The snapshot was not added, I expected to get the original version");
            }
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_retrieving_a_snapshot : PersistenceEngineConcernAsync
    {
        private Snapshot? _correct;
        private ISnapshot? _snapshot;
        private string? _streamId;
        private Snapshot? _tooFarForward;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            var commit1 = await Persistence.CommitSingleAsync(_streamId); // rev 1-2
            var commit2 = await Persistence.CommitNextAsync(commit1!); // rev 3-4
            await Persistence.CommitNextAsync(commit2!); // rev 5-6

            await Persistence.AddSnapshotAsync(new Snapshot(_streamId, 1, string.Empty), CancellationToken.None); //Too far back
            _correct = new Snapshot(_streamId, 3, "Snapshot");
            await Persistence.AddSnapshotAsync(_correct, CancellationToken.None);
            _tooFarForward = new Snapshot(_streamId, 5, string.Empty);
            await Persistence.AddSnapshotAsync(_tooFarForward, CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            _snapshot = await Persistence.GetSnapshotAsync(_streamId!, _tooFarForward!.StreamRevision - 1, CancellationToken.None);
        }

        [Fact]
        public void should_load_the_most_recent_prior_snapshot()
        {
            _snapshot!.StreamRevision.Should().Be(_correct!.StreamRevision);
        }

        [Fact]
        public void should_have_the_correct_snapshot_payload()
        {
            _snapshot!.Payload.Should().Be(_correct!.Payload);
        }

        [Fact]
        public void should_have_the_correct_stream_id()
        {
            _snapshot!.StreamId.Should().Be(_correct!.StreamId);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_snapshot_has_been_added_to_the_most_recent_commit_of_a_stream : PersistenceEngineConcernAsync
    {
        private const string SnapshotData = "snapshot";
        private ICommit? _newest;
        private ICommit? _oldest, _oldest2;
        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            _oldest = await Persistence.CommitSingleAsync(_streamId);
            _oldest2 = await Persistence.CommitNextAsync(_oldest!);
            _newest = await Persistence.CommitNextAsync(_oldest2!);
        }

        protected override Task BecauseAsync()
        {
            return Persistence.AddSnapshotAsync(new Snapshot(_streamId!, _newest!.StreamRevision, SnapshotData), CancellationToken.None);
        }

        [Fact]
        public async Task should_no_longer_find_the_stream_in_the_set_of_streams_to_be_snapshot()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(1, observer, CancellationToken.None);
            observer.StreamHeads
                .Any(x => x.StreamId == _streamId).Should().BeFalse();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_adding_a_commit_after_a_snapshot : PersistenceEngineConcernAsync
    {
        private const int WithinThreshold = 2;
        private const int OverThreshold = 3;
        private const string SnapshotData = "snapshot";
        private ICommit? _oldest, _oldest2;
        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            _oldest = await Persistence.CommitSingleAsync(_streamId);
            _oldest2 = await Persistence.CommitNextAsync(_oldest!);
            await Persistence.AddSnapshotAsync(new Snapshot(_streamId, _oldest2!.StreamRevision, SnapshotData), CancellationToken.None);
        }

        protected override Task BecauseAsync()
        {
            return Persistence.CommitAsync(_oldest2!.BuildNextAttempt(), CancellationToken.None);
        }

        // Because Raven and Mongo update the stream head asynchronously, this test will occasionally fail
        [Fact]
        public async Task should_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_within_the_threshold()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(WithinThreshold, observer, CancellationToken.None);
            observer.StreamHeads
                .FirstOrDefault(x => x.StreamId == _streamId).Should().NotBeNull();
        }

        [Fact]
        public async Task should_not_find_the_stream_in_the_set_of_streams_to_be_snapshot_when_over_the_threshold()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(OverThreshold, observer, CancellationToken.None);
            observer.StreamHeads
                .Any(x => x.StreamId == _streamId).Should().BeFalse();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_commits_from_a_particular_point_in_time : PersistenceEngineConcernAsync
    {
        private ICommit[]? _committed;
        private CommitAttempt? _first;
        private DateTime _now;
        private ICommit? _second;
        private string? _streamId;
        private ICommit? _third;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();

            _now = SystemTime.UtcNow.AddYears(1);
            _first = _streamId.BuildAttempt(_now.AddSeconds(1));
            await Persistence.CommitAsync(_first, CancellationToken.None);

            _second = await Persistence.CommitNextAsync(_first);
            _third = await Persistence.CommitNextAsync(_second!);
            await Persistence.CommitNextAsync(_third!);
        }

        protected override void Because()
        {
            _committed = Persistence.GetFrom(Bucket.Default, _now).ToArray();
        }

        [Fact]
        public void should_return_all_commits_on_or_after_the_point_in_time_specified()
        {
            _committed!.Length.Should().Be(4);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_from_a_particular_point_in_time : PersistenceEngineConcernAsync
    {
        private CommitAttempt[]? _committed;
        private ICommit[]? _loaded;
        private DateTime _start;

        protected override async Task ContextAsync()
        {
            _start = SystemTime.UtcNow;
            // Due to loss in precision in various storage engines, we're rounding down to the
            // nearest second to ensure include all commits from the 'start'.
            _start = _start.AddSeconds(-1);
            _committed = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 2)).ToArray();
        }

        protected override void Because()
        {
            _loaded = Persistence.GetFrom(Bucket.Default, _start).ToArray();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted()
        {
            _loaded!.Length.Should().Be(_committed!.Length);
        }

        [Fact]
        public void should_load_the_same_commits_which_have_been_persisted()
        {
            _committed!
                .All(commit => _loaded!.SingleOrDefault(loaded => loaded.CommitId == commit.CommitId) != null)
                .Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_from_a_particular_checkpoint : PersistenceEngineConcernAsync
    {
        private List<Guid>? _committed;
        private List<Guid>? _loaded;
        private const int checkPoint = 2;

        protected override async Task ContextAsync()
        {
            _committed = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1)).Select(c => c.CommitId).ToList();
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(checkPoint, observer, CancellationToken.None).ConfigureAwait(false);
            _loaded = observer.Commits.Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint()
        {
            _loaded!.Count.Should().Be(_committed!.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_starting_from_the_checkpoint()
        {
            _committed!.Skip(checkPoint).All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_of_a_bucket_from_a_particular_checkpoint : PersistenceEngineConcernAsync
    {
        private List<Guid>? _committedOnBucket1;
        private List<Guid>? _committedOnBucket2;
        private List<Guid>? _loaded;
        private const int checkPoint = 2;

        protected override async Task ContextAsync()
        {
            _committedOnBucket1 = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1, null, "b1")).Select(c => c.CommitId).ToList();
            _committedOnBucket2 = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1, null, "b2")).Select(c => c.CommitId).ToList();
            _committedOnBucket1.AddRange((await Persistence.CommitManyAsync(4, null, "b1")).Select(c => c.CommitId));
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync("b1", checkPoint, observer, CancellationToken.None).ConfigureAwait(false);
            _loaded = observer.Commits.Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint()
        {
            _loaded!.Count.Should().Be(_committedOnBucket1!.Count - checkPoint);
        }

        [Fact]
        public void should_load_only_the_commits_on_bucket1_starting_from_the_checkpoint()
        {
            _committedOnBucket1!.Skip(checkPoint).All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }

        [Fact]
        public void should_not_load_the_commits_from_bucket2()
        {
            _committedOnBucket2!.All(x => !_loaded!.Contains(x)).Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_from_a_particular_checkpoint_to_a_checkpoint : PersistenceEngineConcernAsync
    {
        private readonly List<Guid> _committed = [];
        private List<Guid>? _loaded;
        private const int startCheckpoint = 2;
        private int endCheckpoint;

        protected override async Task ContextAsync()
        {
            var committedOnBucket1 = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1, null, Bucket.Default)).Select(c => c.CommitId).ToList();
            var committedOnBucket2 = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1, null, "Bucket1")).Select(c => c.CommitId).ToList();
            _committed.AddRange(committedOnBucket1);
            _committed.AddRange(committedOnBucket2);
            endCheckpoint = (2 * (ConfiguredPageSizeForTesting + 1)) - 1;
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromToAsync(startCheckpoint, endCheckpoint, observer, CancellationToken.None).ConfigureAwait(false);
            _loaded = observer.Commits.Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _loaded!.Count.Should().Be(endCheckpoint - startCheckpoint);
        }

        [Fact]
        public void should_load_only_the_commits_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _committed
                .Skip(startCheckpoint)
                .Take(_committed.Count - startCheckpoint - 1)
                //.Take(endCheckpoint - startCheckpoint)
                .All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_paging_over_all_commits_of_a_bucket_from_a_particular_checkpoint_to_a_checkpoint : PersistenceEngineConcernAsync
    {
        private List<Guid>? _committedOnBucket1;
        private List<Guid>? _committedOnBucket2;
        private List<Guid>? _loaded;
        private const int startCheckpoint = 2;
        private int endCheckpoint;

        protected override async Task ContextAsync()
        {
            _committedOnBucket1 = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1, null, "b1")).Select(c => c.CommitId).ToList();
            _committedOnBucket2 = (await Persistence.CommitManyAsync(ConfiguredPageSizeForTesting + 1, null, "b2")).Select(c => c.CommitId).ToList();
            _committedOnBucket1.AddRange((await Persistence.CommitManyAsync(4, null, "b1")).Select(c => c.CommitId));
            endCheckpoint = ((2 * (ConfiguredPageSizeForTesting + 1)) + 4) - 1;
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromToAsync("b1", startCheckpoint, endCheckpoint, observer, CancellationToken.None).ConfigureAwait(false);
            _loaded = observer.Commits.Select(c => c.CommitId).ToList();
        }

        [Fact]
        public void should_load_the_same_number_of_commits_which_have_been_persisted_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _loaded!.Count.Should().Be(_committedOnBucket1!.Count - startCheckpoint - 1);
        }

        [Fact]
        public void should_load_only_the_commits_on_bucket1_starting_from_the_checkpoint_to_the_checkpoint()
        {
            _committedOnBucket1!
                .Skip(startCheckpoint)
                .Take(_committedOnBucket1!.Count - startCheckpoint - 1)
                .All(x => _loaded!.Contains(x)).Should().BeTrue(); // all commits should be found in loaded collection
        }

        [Fact]
        public void should_not_load_the_commits_from_bucket2()
        {
            _committedOnBucket2!.All(x => !_loaded!.Contains(x)).Should().BeTrue();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_commits_from_the_year_1_AD : PersistenceEngineConcernAsync
    {
        private Exception? _thrown;

        protected override async Task BecauseAsync()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _thrown = await Catch.ExceptionAsync(() => Persistence.GetFromAsync(Bucket.Default, 0, new CommitStreamObserver(), CancellationToken.None));
        }

        [Fact]
        public void should_NOT_throw_an_exception()
        {
            _thrown.Should().BeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_all_commits : PersistenceEngineConcernAsync
    {
        protected override Task ContextAsync()
        {
            return Persistence.CommitSingleAsync();
        }

        protected override Task BecauseAsync()
        {
            return Persistence.PurgeAsync(CancellationToken.None);
        }

        [Fact]
        public async Task should_not_find_any_commits_stored()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(Bucket.Default, 0, observer, CancellationToken.None).ConfigureAwait(false);
            observer.Commits.Count.Should().Be(0);
        }

        [Fact]
        public async Task should_not_find_any_streams_to_snapshot()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(0, observer, CancellationToken.None);
            observer.StreamHeads
                .Count.Should().Be(0);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_invoking_after_disposal : PersistenceEngineConcernAsync
    {
        private Exception? _thrown;

        protected override void Context()
        {
            Persistence.Dispose();
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitSingleAsync());
        }

        [Fact]
        public void should_throw_an_ObjectDisposedException()
        {
            _thrown.Should().BeOfType<ObjectDisposedException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_id_as_a_stream_same_bucket : PersistenceEngineConcernAsync
    {
        private string? _streamId;
        private static Exception? _thrown;

        protected override Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            return Persistence.CommitAsync(_streamId.BuildAttempt(), CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_streamId!.BuildAttempt(), CancellationToken.None));
        }

        [Fact]
        public void should_throw()
        {
            _thrown.Should().NotBeNull();
        }

        [Fact]
        public void should_be_duplicate_commit_exception()
        {
            _thrown.Should().BeOfType<ConcurrencyException>();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_committing_a_stream_with_the_same_id_as_a_stream_in_another_bucket : PersistenceEngineConcernAsync
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";
        private string? _streamId;
        private static CommitAttempt? _attemptForBucketB;
        private static Exception? _thrown;
        private DateTime _attemptACommitStamp;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            DateTime now = SystemTime.UtcNow;
            await Persistence.CommitAsync(_streamId.BuildAttempt(now, _bucketAId), CancellationToken.None);

            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_bucketAId, _streamId, 0, int.MaxValue, observer, CancellationToken.None);
            _attemptACommitStamp = observer.Commits[0].CommitStamp;

            _attemptForBucketB = _streamId.BuildAttempt(now.Subtract(TimeSpan.FromDays(1)), _bucketBId);
        }

        protected override async Task BecauseAsync()
        {
            _thrown = await Catch.ExceptionAsync(() => Persistence.CommitAsync(_attemptForBucketB!, CancellationToken.None));
        }

        [Fact]
        public void should_succeed()
        {
            _thrown.Should().BeNull();
        }

        [Fact]
        public async Task should_persist_to_the_correct_bucket()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_bucketBId, _streamId!, 0, int.MaxValue, observer, CancellationToken.None);
            var stream = observer.Commits;

            stream.Should().NotBeNull();
            stream.Count.Should().Be(1);
        }

        [Fact]
        public async Task should_not_affect_the_stream_from_the_other_bucket()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_bucketAId, _streamId!, 0, int.MaxValue, observer, CancellationToken.None);
            var stream = observer.Commits;

            stream.Should().NotBeNull();
            stream.Count.Should().Be(1);
            stream[0].CommitStamp.Should().Be(_attemptACommitStamp);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_saving_a_snapshot_for_a_stream_with_the_same_id_as_a_stream_in_another_bucket : PersistenceEngineConcernAsync
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";

        private string? _streamId;

        private static Snapshot? _snapshot;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            _snapshot = new Snapshot(_bucketBId, _streamId, 1, "Snapshot");
            await Persistence.CommitAsync(_streamId.BuildAttempt(bucketId: _bucketAId), CancellationToken.None);
            await Persistence.CommitAsync(_streamId.BuildAttempt(bucketId: _bucketBId), CancellationToken.None);
        }

        protected override Task BecauseAsync()
        {
            return Persistence.AddSnapshotAsync(_snapshot!, CancellationToken.None);
        }

        [Fact]
        public async Task should_affect_snapshots_from_another_bucket()
        {
            (await Persistence.GetSnapshotAsync(_bucketAId, _streamId!, _snapshot!.StreamRevision, CancellationToken.None)).Should().BeNull();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_reading_all_commits_from_a_particular_point_in_time_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcernAsync
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";

        private static DateTime _now;
        private static ICommit[]? _returnedCommits;
        private CommitAttempt? _commitToBucketB;

        protected override async Task ContextAsync()
        {
            _now = SystemTime.UtcNow.AddYears(1);

            var commitToBucketA = Guid.NewGuid().ToString().BuildAttempt(_now.AddSeconds(1), _bucketAId);

            await Persistence.CommitAsync(commitToBucketA, CancellationToken.None);
            commitToBucketA = commitToBucketA.BuildNextAttempt();
            await Persistence.CommitAsync(commitToBucketA, CancellationToken.None);
            commitToBucketA = commitToBucketA.BuildNextAttempt();
            await Persistence.CommitAsync(commitToBucketA, CancellationToken.None);
            await Persistence.CommitAsync(commitToBucketA.BuildNextAttempt(), CancellationToken.None);

            _commitToBucketB = Guid.NewGuid().ToString().BuildAttempt(_now.AddSeconds(1), _bucketBId);

            await Persistence.CommitAsync(_commitToBucketB, CancellationToken.None);
        }

        protected override void Because()
        {
            _returnedCommits = Persistence.GetFrom(_bucketAId, _now).ToArray();
        }

        [Fact]
        public void should_not_return_commits_from_other_buckets()
        {
            _returnedCommits!.Any(c => c.CommitId.Equals(_commitToBucketB!.CommitId)).Should().BeFalse();
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_getting_all_commits_since_checkpoint_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcernAsync
    {
        private ICommit[]? _commits;

        protected override async Task ContextAsync()
        {
            const string bucketAId = "a";
            const string bucketBId = "b";
            await Persistence.CommitAsync(Guid.NewGuid().ToString().BuildAttempt(bucketId: bucketAId), CancellationToken.None);
            await Persistence.CommitAsync(Guid.NewGuid().ToString().BuildAttempt(bucketId: bucketBId), CancellationToken.None);
            await Persistence.CommitAsync(Guid.NewGuid().ToString().BuildAttempt(bucketId: bucketAId), CancellationToken.None);
        }

        protected override async Task BecauseAsync()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(0, observer, CancellationToken.None).ConfigureAwait(false);
            _commits = observer.Commits.ToArray();
        }

        [Fact]
        public void should_not_be_empty()
        {
            _commits.Should().NotBeEmpty();
        }

        [Fact]
        public void should_be_in_order_by_checkpoint()
        {
            Int64 checkpoint = 0;
            foreach (var commit in _commits!)
            {
                Int64 commitCheckpoint = commit.CheckpointToken;
                commitCheckpoint.Should().BeGreaterThan(checkpoint);
                checkpoint = commit.CheckpointToken;
            }
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_purging_all_commits_and_there_are_streams_in_multiple_buckets : PersistenceEngineConcernAsync
    {
        private const string _bucketAId = "a";
        private const string _bucketBId = "b";

        private string? _streamId;

        protected override async Task ContextAsync()
        {
            _streamId = Guid.NewGuid().ToString();
            await Persistence.CommitAsync(_streamId.BuildAttempt(bucketId: _bucketAId), CancellationToken.None);
            await Persistence.CommitAsync(_streamId.BuildAttempt(bucketId: _bucketBId), CancellationToken.None);
            var _snapshotA = new Snapshot(bucketId: _bucketAId, _streamId, 1, "SnapshotA");
            await Persistence.AddSnapshotAsync(_snapshotA, CancellationToken.None);
            var _snapshotB = new Snapshot(bucketId: _bucketBId, _streamId, 1, "SnapshotB");
            await Persistence.AddSnapshotAsync(_snapshotB, CancellationToken.None);
        }

        protected override Task BecauseAsync()
        {
            return Persistence.PurgeAsync(CancellationToken.None);
        }

        [Fact]
        public async Task should_purge_all_commits_stored_in_bucket_a()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_bucketAId, 0, observer, CancellationToken.None).ConfigureAwait(false);
            observer.Commits.Count.Should().Be(0);
        }

        [Fact]
        public async Task should_purge_all_commits_stored_in_bucket_b()
        {
            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(_bucketBId, 0, observer, CancellationToken.None).ConfigureAwait(false);
            observer.Commits.Count.Should().Be(0);
        }

        [Fact]
        public async Task should_purge_all_streams_to_snapshot_in_bucket_a()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(_bucketAId, 0, observer, CancellationToken.None);
            observer.StreamHeads.Count.Should().Be(0);
        }

        [Fact]
        public async Task should_purge_all_streams_to_snapshot_in_bucket_b()
        {
            var observer = new StreamHeadObserver();
            await Persistence.GetStreamsToSnapshotAsync(_bucketBId, 0, observer, CancellationToken.None);
            observer.StreamHeads.Count.Should().Be(0);
        }
    }

    [Serializable]
    public class TestEventForAsync
    {
        public String? S { get; set; }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_calling_CommitChanges : PersistenceEngineConcern
    {
        private Guid? _commitId;
        private ICommit? _persistedCommit;
        private ICommit[]? _commits;

        protected override async Task BecauseAsync()
        {
            var eventStore = new OptimisticEventStore(Persistence, null, null);
            using IEventStream stream = await eventStore.OpenStreamAsync(Guid.NewGuid()).ConfigureAwait(false);
            stream.Add(new EventMessage { Body = new TestEvent() { S = "Hi " } });
            _commitId = Guid.NewGuid();
            _persistedCommit = await stream.CommitChangesAsync(_commitId.Value).ConfigureAwait(false);

            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(0, observer, CancellationToken.None).ConfigureAwait(false);
            _commits = observer.Commits.ToArray();
        }

        [Fact]
        public void A_Commit_had_been_persisted()
        {
            _persistedCommit.Should().NotBeNull();
            _persistedCommit!.CommitId.Should().Be(_commitId!.Value);
            _persistedCommit.CommitSequence.Should().Be(1);
            _persistedCommit.Events.Count.Should().Be(1);
            _persistedCommit.Events.Single().Body.Should().BeOfType<TestEvent>();
            ((TestEvent)_persistedCommit.Events.Single().Body!).S.Should().Be("Hi ");
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits!.Length.Should().Be(1);
            // if should have the right event
            _commits[0].CommitId.Should().Be(_commitId!.Value);
            _commits[0].Events.Count.Should().Be(1);
            _commits[0].Events.Single().Body.Should().BeOfType<TestEvent>();
            ((TestEvent)_commits[0].Events.Single().Body!).S.Should().Be("Hi ");
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_gettingFromCheckpoint_amount_of_commits_exceeds_PageSize : PersistenceEngineConcernAsync
    {
        private ICommit[]? _commits;
        private int _moreThanPageSize;

        protected override async Task BecauseAsync()
        {
            _moreThanPageSize = ConfiguredPageSizeForTesting + 1;
            var eventStore = new OptimisticEventStore(Persistence, null, null);
            // TODO: Not sure how to set the actual page size to the const defined above
            for (int i = 0; i < _moreThanPageSize; i++)
            {
                using IEventStream stream = await eventStore.OpenStreamAsync(Guid.NewGuid()).ConfigureAwait(false);
                stream.Add(new EventMessage { Body = new TestEventForAsync() { S = "Hi " + i } });
                await stream.CommitChangesAsync(Guid.NewGuid(), CancellationToken.None).ConfigureAwait(false);
            }

            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(0, observer, CancellationToken.None).ConfigureAwait(false);
            _commits = observer.Commits.ToArray();
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits!.Length.Should().Be(_moreThanPageSize);
        }
    }

#if MSTEST
    [TestClass]
#endif
    public class when_a_payload_is_large : PersistenceEngineConcernAsync
    {
        [Fact]
        public async Task can_commit()
        {
            const int bodyLength = 100000;
            var attempt = new CommitAttempt(
                Bucket.Default,
                Guid.NewGuid().ToString(),
                1,
                Guid.NewGuid(),
                1,
                DateTime.UtcNow,
                new Dictionary<string, object>(),
                [new EventMessage { Body = new string('a', bodyLength) }]);
            await Persistence.CommitAsync(attempt, CancellationToken.None);

            var observer = new CommitStreamObserver();
            await Persistence.GetFromAsync(0, observer, CancellationToken.None).ConfigureAwait(false);
            ICommit commits = observer.Commits.Single();

            commits.Events.Single().Body.ToString()!.Length.Should().Be(bodyLength);
        }
    }

    /// <summary>
    /// We are adapting the tests to use 3 different frameworks:
    /// - XUnit: the attached test runner does the job (fixture setup and cleanup)
    /// - NUnit (.net core project)
    /// - MSTest (.net core project)
    /// </summary>
    public abstract class PersistenceEngineConcernAsync : SpecificationBase
#if XUNIT
        , IUseFixture<PersistenceEngineFixture>
#endif
#if NUNIT || MSTEST
        , IDisposable
#endif
    {
        protected PersistenceEngineFixtureAsync? Fixture { get; private set; }

        protected IPersistStreams Persistence
        {
            get { return Fixture!.Persistence!; }
        }

        protected int ConfiguredPageSizeForTesting
        {
            get { return 2; }
        }

        /// <summary>
        /// Can be used by XUNIT to initialize the tests.
        /// </summary>
        public void SetFixture(PersistenceEngineFixtureAsync data)
        {
            Fixture = data;
            Fixture.Initialize(ConfiguredPageSizeForTesting);
        }

#if NUNIT || MSTEST
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Fixture?.Dispose();
        }

        /// <summary>
        /// <para>
        /// This code was meant to be run right before every test in the fixture to give time
        /// to do further initialization before the PersistenceEngineFixture was created.
        /// Unfortunately the 3 frameworks
        /// have very different ways of doing this:
        /// - NUnit: TestFixtureSetUp
        /// - MSTest: ClassInitialize (not inherited, will be ignored if defined on a base class)
        /// - xUnit: IUseFixture + SetFixture
        /// We need a way to also have some configuration before the PersistenceEngineFixture is created.
        /// </para>
        /// <para>
        /// We've decided to use the test constructor to do the job, it's your responsibility to guarantee
        /// One time initialization (for anything that need it, if you have multiple tests on a fixture)
        /// depending on the framework you are using.
        /// </para>
        /// <para>
        /// quick workaround:
        /// - change the parameters of the "Fixture" properties.
        /// - call the 'Reinitialize()' method can be called to rerun the initialization after we changed the configuration
        /// in the constructor.
        /// or
        /// - call the SetFixture() to reinitialize everything.
        /// </para>
        /// </summary>
        protected PersistenceEngineConcernAsync() : this(new PersistenceEngineFixtureAsync())
        { }

        protected PersistenceEngineConcernAsync(PersistenceEngineFixtureAsync fixture)
        {
            SetFixture(fixture);
        }
#endif
    }

    public partial class PersistenceEngineFixtureAsync : IDisposable
    {
        public IPersistStreams? Persistence { get; private set; }

        private readonly Func<int, IPersistStreams> _createPersistence;

#if NET462
        private bool _tracking = false;
        private string _trackingInstanceName;

        /// <summary>
        /// Automatic Performance Counters and tracking was disabled for full
        /// framework tests because their initialization
        /// can fail when the tests run on build machines (like AppVeyor and similar).
        /// You can enable it back calling this function before <see cref="Initialize(int)"/>
        /// </summary>
        /// <param name="instanceName"></param>
        /// <returns></returns>
        public PersistenceEngineFixture TrackPerformanceInstance(string instanceName = "tests")
        {
            _trackingInstanceName = instanceName;
            _tracking = true;
            return this;
        }
#endif

        public void Initialize(int pageSize)
        {
#if NET462
            // performance counters cab be disabled for full framework tests because their initialization
            // can fail when the tests run on build machines (like AppVeyor and similar)
            if (_tracking)
            {
                Persistence = new NEventStore.Diagnostics.PerformanceCounterPersistenceEngine(_createPersistence(pageSize), _trackingInstanceName);
            }
            else
            {
                Persistence = _createPersistence(pageSize);
            }
#else
            Persistence = _createPersistence(pageSize);
#endif
            Persistence.Initialize();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Persistence?.IsDisposed == false)
            {
                Persistence.Drop();
                Persistence.Dispose();
            }
        }
    }
}

#pragma warning restore 169 // ReSharper disable InconsistentNaming
#pragma warning restore IDE1006 // Naming Styles
