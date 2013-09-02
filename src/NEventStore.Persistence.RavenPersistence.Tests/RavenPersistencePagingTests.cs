
#pragma warning disable 169

namespace NEventStore.Persistence.RavenPersistence.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NEventStore.Persistence.AcceptanceTests;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_getting_paged_commits : PersistenceEngineConcern
    {
        private const int NumberOfCommits = 30;
        private ICommit[] _results;
        private DateTime _start;

        protected override void Context()
        {
            _start = SystemTime.UtcNow;
            Persistence.CommitMany(NumberOfCommits);
        }

        protected override void Because()
        {
            _results = Persistence.GetFrom(_start).ToArray();
        }

        [Fact]
        public void should_return_all_commits()
        {
            _results.Length.ShouldBe(NumberOfCommits);
        }
    }

    public class when_getting_paged_commits_and_enumerating_multiple_times : PersistenceEngineConcern
    {
        private const int NumberOfCommits = 30;
        private Exception _exception;
        private ICommit[] _firstPage, _secondPage;
        private DateTime _start;

        protected override void Context()
        {
            _start = DateTime.UtcNow;
            Persistence.CommitMany(NumberOfCommits);
        }

        protected override void Because()
        {
            IEnumerable<ICommit> enumerable = Persistence.GetFrom(_start);
            _firstPage = enumerable.Take(10).ToArray();
            _secondPage = enumerable.ToArray();
        }

        [Fact]
        public void should_return_the_items_for_the_first_page()
        {
            _firstPage.Length.ShouldBe(10);
        }

        [Fact]
        public void should_restart_paging()
        {
            _secondPage.Length.ShouldBe(30);
        }
    }

    public class when_getting_paged_commits_and_a_subsequent_page_throws_an_error : SpecificationBase,
                                                                                    IUseFixture<RavenPersistenceEngineFixture>
    {
        private const int NumberOfCommits = 30;
        private Exception _exception;
        private ICommit[] _firstPage;
        private RavenPersistenceEngine _persistence;
        private DateTime _start;

        public void SetFixture(RavenPersistenceEngineFixture data)
        {
            _persistence = data.Persistence;
        }

        protected override void Context()
        {
            _start = DateTime.UtcNow;
            _persistence.CommitMany(NumberOfCommits);
        }

        protected override void Because()
        {
            IEnumerable<ICommit> enumerable = _persistence.GetFrom(_start);
            _firstPage = enumerable.Take(10).ToArray();
            _persistence.Store.Dispose();

            //TODO:Use a on-demand raven server and shut down to create a web exception
            _exception = Catch.Exception(() => enumerable.Take(10).ToArray());
        }

        [Fact]
        public void should_return_the_items_for_the_first_page()
        {
            _firstPage.Length.ShouldBe(10);
        }

        [Fact]
        public void should_throw_a_storage_exception()
        {
            _exception.ShouldBeInstanceOf<ObjectDisposedException>();
        }
    }
}

#pragma warning restore 169