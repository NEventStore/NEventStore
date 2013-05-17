using System;
using System.Linq;
using EventStore.Persistence.AcceptanceTests.Engines;
using EventStore.Persistence.RavenPersistence;
using Machine.Specifications;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Persistence.AcceptanceTests.RavenPersistence
{
    [Subject("RavenPersistence - Paging")]
    public class when_getting_paged_commits : with_in_memory_raven_persistence
    {
        static DateTime start;
        static Commit[] results;
        const int NumberOfCommits = 30;

        Establish context = () =>
        {
            start = SystemTime.UtcNow;
            persistence.CommitMany(NumberOfCommits);
        };

        Because of = () =>
            results = persistence.GetFrom(start).ToArray();

        It should_return_all_commits = () =>
            results.Length.ShouldEqual(NumberOfCommits);
    }

    [Subject("RavenPersistence - Paging")]
    public class when_getting_paged_commits_and_enumerating_multiple_times : with_in_memory_raven_persistence
    {
        static DateTime start;
        static Commit[] firstPage, secondPage;
        const int NumberOfCommits = 30;
        static Exception exception;

        Establish context = () =>
        {
            start = DateTime.UtcNow;
            persistence.CommitMany(NumberOfCommits);
        };

        Because of = () =>
        {
            var enumerable = persistence.GetFrom(start);
            firstPage = enumerable.Take(10).ToArray();
            secondPage = enumerable.ToArray();
        };

        It should_return_the_items_for_the_first_page = () =>
            firstPage.Length.ShouldEqual(10);

        It should_restart_paging = () =>
            secondPage.Length.ShouldEqual(30);
    }

    [Subject("RavenPersistence - Paging")]
    public class when_getting_paged_commits_and_a_subsequent_page_throws_an_error : with_in_memory_raven_persistence
    {
        static DateTime start;
        static Commit[] firstPage;
        const int NumberOfCommits = 30;
        static Exception exception;

        Establish context = () =>
        {
            start = DateTime.UtcNow;
            persistence.CommitMany(NumberOfCommits);
        };

        Because of = () =>
        {
            var enumerable = persistence.GetFrom(start);
            firstPage = enumerable.Take(10).ToArray();
            persistence.Store.Dispose();
            
            //TODO:Use a on-demand raven server and shut down to create a web exception
            exception = Catch.Exception(() => enumerable.Take(10).ToArray());
        };

        It should_return_the_items_for_the_first_page = () =>
            firstPage.Length.ShouldEqual(10);

        It should_throw_a_storage_exception = () =>
            exception.ShouldBeOfType<ObjectDisposedException>();
    }

    public class with_in_memory_raven_persistence
    {
        protected static RavenPersistenceEngine persistence;

        Establish context = () =>
        {
            var config = TestRavenConfig.GetDefaultConfig();
            config.RequestedPageSize = 10;

            persistence = (RavenPersistenceEngine)new InMemoryRavenPersistenceFactory(config).Build();
            persistence.Initialize();
        };

        Cleanup cleanup = () =>
        {
            if (persistence != null)
                persistence.Dispose();
        };
    }

}

// ReSharper enable InconsistentNaming
#pragma warning restore 169