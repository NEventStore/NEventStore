using System;
using System.Linq;
using Xunit;
using Xunit.Should;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.RavenPersistence.Tests
{
    using NEventStore;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;

    public class when_getting_paged_commits : PersistenceEngineConcern
    {
        DateTime start;
        Commit[] results;
        const int NumberOfCommits = 30;

        protected override void Context()
        {
            start = SystemTime.UtcNow;
            Persistence.CommitMany(NumberOfCommits);
        }

        protected override void Because()
        {
            results = Persistence.GetFrom(start).ToArray();
        }

        [Fact]
        public void should_return_all_commits()
        {
            results.Length.ShouldBe(NumberOfCommits);
        }
    }

    public class when_getting_paged_commits_and_enumerating_multiple_times : PersistenceEngineConcern
    {
        DateTime start;
        Commit[] firstPage, secondPage;
        const int NumberOfCommits = 30;
        Exception exception;

        protected override void Context()
        {
            start = DateTime.UtcNow;
            Persistence.CommitMany(NumberOfCommits);
        }

        protected override void Because()
        {
            var enumerable = Persistence.GetFrom(start);
            firstPage = enumerable.Take(10).ToArray();
            secondPage = enumerable.ToArray();
        }

        [Fact]
        public void should_return_the_items_for_the_first_page()
        {
            firstPage.Length.ShouldBe(10);
        }

        [Fact]
        public void should_restart_paging()
        {
            secondPage.Length.ShouldBe(30);
        }
    }

    public class when_getting_paged_commits_and_a_subsequent_page_throws_an_error : SpecificationBase, IUseFixture<RavenPersistenceEngineFixture>
    {
        DateTime start;
        Commit[] firstPage;
        const int NumberOfCommits = 30;
        Exception exception;
        RavenPersistenceEngine persistence;

        public void SetFixture(RavenPersistenceEngineFixture data)
        {
            persistence = data.Persistence;
        }

        protected override void Context()
        {
            start = DateTime.UtcNow;
            persistence.CommitMany(NumberOfCommits);
        }

        protected override void Because()
        {
            var enumerable = persistence.GetFrom(start);
            firstPage = enumerable.Take(10).ToArray();
            persistence.Store.Dispose();
            
            //TODO:Use a on-demand raven server and shut down to create a web exception
            exception = Catch.Exception(() => enumerable.Take(10).ToArray());
        }
        
        [Fact]
        public void should_return_the_items_for_the_first_page()
        {
            firstPage.Length.ShouldBe(10);
        }

        [Fact]
        public void should_throw_a_storage_exception()
        {
            exception.ShouldBeInstanceOf<ObjectDisposedException>();
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169