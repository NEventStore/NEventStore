#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.Persistence.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using FluentAssertions;
#if MSTEST
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
    using NUnit.Framework;
    using System.Threading.Tasks;
    using System.Transactions;
    using System.Threading;
    using System.Globalization;
#endif
#if XUNIT
    using Xunit;
    using Xunit.Should;
#endif

    /* Transactions support must be investigated, it should be valid only for Databases that supports it (InMemoryPersistence will not). */
#if MSTEST
    [TestClass]
#endif
    public class TransactionConcern : PersistenceEngineConcern
    {
        private ICommit[] _commits;
        private const int Loop = 2;
        private const int StreamsPerTransaction = 20;

        protected override void Because()
        {
            Parallel.For(0, Loop, i =>
            {
                var eventStore = new OptimisticEventStore(Persistence, null);
                using (var scope = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
                {
                    int j;
                    for (j = 0; j < StreamsPerTransaction; j++)
                    {
                        using (var stream = eventStore.OpenStream(i.ToString() + "-" + j.ToString()))
                        {
                            for (int k = 0; k < 10; k++)
                            {
                                stream.Add(new EventMessage { Body = "body" + k });
                            }
                            stream.CommitChanges(Guid.NewGuid());
                        }
                    }
                    scope.Complete();
                }
            });
            _commits = Persistence.GetFrom().ToArray();
        }

        [Fact]
        public void Should_have_expected_number_of_commits()
        {
            _commits.Length.Should().Be(Loop * StreamsPerTransaction);
        }

        [Fact]
        public void ScopeCompleteAndSerializable()
        {
            Reinitialize();
            const int loop = 10;
            using (var scope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.Serializable
                }))
            {
                Parallel.For(0, loop, i =>
                {
                    Console.WriteLine("Creating stream {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                    var eventStore = new OptimisticEventStore(Persistence, null);
                    string streamId = i.ToString(CultureInfo.InvariantCulture);
                    using (var stream = eventStore.OpenStream(streamId))
                    {
                        stream.Add(new EventMessage { Body = "body1" });
                        stream.Add(new EventMessage { Body = "body2" });
                        stream.CommitChanges(Guid.NewGuid());
                    }
                });
                scope.Complete();
            }
            ICommit[] commits = Persistence.GetFrom(0).ToArray();
            commits.Length.Should().Be(loop);
        }

        [Fact]
        public void ScopeNotCompleteAndReadCommitted()
        {
            Reinitialize();
            const int loop = 10;
            using (new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
            {
                Parallel.For(0, loop, i =>
                {
                    Console.WriteLine(@"Creating stream {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                    var eventStore = new OptimisticEventStore(Persistence, null);
                    string streamId = i.ToString(CultureInfo.InvariantCulture);
                    using (var stream = eventStore.OpenStream(streamId))
                    {
                        stream.Add(new EventMessage { Body = "body1" });
                        stream.Add(new EventMessage { Body = "body2" });
                        stream.CommitChanges(Guid.NewGuid());
                    }
                });
            }
            ICommit[] commits = Persistence.GetFrom(0).ToArray();
            commits.Length.Should().Be(0);
        }

        [Fact]
        public void ScopeNotCompleteAndSerializable()
        {
            Reinitialize();
            const int loop = 10;
            using (new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }))
            {
                Parallel.For(0, loop, i =>
                {
                    Console.WriteLine(@"Creating stream {0} on thread {1}", i, Thread.CurrentThread.ManagedThreadId);
                    var eventStore = new OptimisticEventStore(Persistence, null);
                    string streamId = i.ToString(CultureInfo.InvariantCulture);
                    using (var stream = eventStore.OpenStream(streamId))
                    {
                        stream.Add(new EventMessage { Body = "body1" });
                        stream.Add(new EventMessage { Body = "body2" });
                        stream.CommitChanges(Guid.NewGuid());
                    }
                });
            }
            ICommit[] commits = Persistence.GetFrom(0).ToArray();
            commits.Length.Should().Be(0);
        }
    }

    // ReSharper restore InconsistentNaming
}
