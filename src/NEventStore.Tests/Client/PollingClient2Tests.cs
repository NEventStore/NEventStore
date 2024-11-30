#pragma warning disable IDE1006 // Naming Styles

#if MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FakeItEasy;
using FluentAssertions;
using NEventStore.Persistence;
using NEventStore.Persistence.AcceptanceTests;
using NEventStore.Persistence.AcceptanceTests.BDD.NUnit;
using NEventStore.PollingClient;
#if XUNIT
	using Xunit;
	using Xunit.Should;
#endif

namespace NEventStore.Tests.Client;
#if MSTEST
	[TestClass]
#endif
public class CreatingPollingClient2Tests
{
    [Fact]
    public void When_persist_streams_is_null_then_should_throw()
    {
        Catch.Exception(() => new PollingClient2(null, _ => PollingClient2.HandlingResult.MoveToNext)).Should()
            .BeOfType<ArgumentNullException>();
    }

    [Fact]
    public void When_interval_less_than_zero_then_should_throw()
    {
        Catch.Exception(() => new PollingClient2(A.Fake<IPersistStreams>(), null)).Should()
            .BeOfType<ArgumentNullException>();
    }
}

#if MSTEST
	[TestClass]
#endif
public class base_handling_committed_events : using_polling_client2
{
    private readonly List<ICommit> commits = new();

    protected override void Context()
    {
        base.Context();
        HandleFunction = c =>
        {
            commits.Add(c);
            return PollingClient2.HandlingResult.MoveToNext;
        };
        StoreEvents.Advanced.CommitSingle();
    }

    protected override void Because()
    {
        Sut.StartFrom();
    }

    [Fact]
    public void commits_are_correctly_dispatched()
    {
        WaitForCondition(() => commits.Count >= 1);
        commits.Count.Should().Be(1);
    }
}

#if MSTEST
	[TestClass]
#endif
public class base_handling_committed_events_and_new_events : using_polling_client2
{
    private readonly List<ICommit> commits = new();

    protected override void Context()
    {
        base.Context();
        HandleFunction = c =>
        {
            commits.Add(c);
            return PollingClient2.HandlingResult.MoveToNext;
        };
        StoreEvents.Advanced.CommitSingle();
    }

    protected override void Because()
    {
        Sut.StartFrom();
        for (var i = 0; i < 15; i++) StoreEvents.Advanced.CommitSingle();
    }

    [Fact]
    public void commits_are_correctly_dispatched()
    {
        WaitForCondition(() => commits.Count >= 16);
        commits.Count.Should().Be(16);
    }
}

#if MSTEST
	[TestClass]
#endif
public class verify_stopping_commit_polling_client : using_polling_client2
{
    private readonly List<ICommit> commits = new();

    protected override void Context()
    {
        base.Context();
        HandleFunction = c =>
        {
            commits.Add(c);
            return PollingClient2.HandlingResult.Stop;
        };
        StoreEvents.Advanced.CommitSingle();
        StoreEvents.Advanced.CommitSingle();
        StoreEvents.Advanced.CommitSingle();
    }

    protected override void Because()
    {
        Sut.StartFrom();
    }

    [Fact]
    public void commits_are_correctly_dispatched()
    {
        WaitForCondition(() => commits.Count >= 2, 1);
        commits.Count.Should().Be(1);
    }
}

#if MSTEST
	[TestClass]
#endif
public class verify_retry_commit_polling_client : using_polling_client2
{
    private readonly List<ICommit> commits = new();

    protected override void Context()
    {
        base.Context();
        HandleFunction = c =>
        {
            commits.Add(c);
            if (commits.Count < 3)
                return PollingClient2.HandlingResult.Retry;

            return PollingClient2.HandlingResult.MoveToNext;
        };
        StoreEvents.Advanced.CommitSingle();
    }

    protected override void Because()
    {
        Sut.StartFrom();
    }

    [Fact]
    public void commits_are_retried()
    {
        WaitForCondition(() => commits.Count >= 3, 1);
        commits.Count.Should().Be(3);
        commits.All(c => c.CheckpointToken == 1).Should().BeTrue();
    }
}

#if MSTEST
	[TestClass]
#endif
public class verify_retry_then_move_next : using_polling_client2
{
    private readonly List<ICommit> commits = new();

    protected override void Context()
    {
        base.Context();
        HandleFunction = c =>
        {
            commits.Add(c);
            if (commits.Count < 3 && c.CheckpointToken == 1)
                return PollingClient2.HandlingResult.Retry;

            return PollingClient2.HandlingResult.MoveToNext;
        };
        StoreEvents.Advanced.CommitSingle();
        StoreEvents.Advanced.CommitSingle();
    }

    protected override void Because()
    {
        Sut.StartFrom();
    }

    [Fact]
    public void commits_are_retried_then_move_next()
    {
        WaitForCondition(() => commits.Count >= 4, 1);
        commits.Count.Should().Be(4);
        commits
            .Select(c => c.CheckpointToken)
            .SequenceEqual(new[] { 1L, 1L, 1, 2 })
            .Should().BeTrue();
    }
}

#if MSTEST
	[TestClass]
#endif
public class verify_manual_plling : using_polling_client2
{
    private readonly List<ICommit> commits = new();

    protected override void Context()
    {
        base.Context();
        HandleFunction = c =>
        {
            commits.Add(c);
            return PollingClient2.HandlingResult.MoveToNext;
        };
        StoreEvents.Advanced.CommitSingle();
        StoreEvents.Advanced.CommitSingle();
    }

    protected override void Because()
    {
        Sut.ConfigurePollingFunction();
        Sut.PollNow();
    }

    [Fact]
    public void commits_are_retried_then_move_next()
    {
        WaitForCondition(() => commits.Count >= 2, 3);
        commits.Count.Should().Be(2);
        commits
            .Select(c => c.CheckpointToken)
            .SequenceEqual(new[] { 1L, 2L })
            .Should().BeTrue();
    }
}

public abstract class using_polling_client2 : SpecificationBase
{
    protected const int PollingInterval = 100;

    protected Func<ICommit, PollingClient2.HandlingResult> HandleFunction;
    protected PollingClient2 sut;

    protected PollingClient2 Sut => sut;

    protected IStoreEvents StoreEvents { get; private set; }

    protected override void Context()
    {
        HandleFunction = _ => PollingClient2.HandlingResult.MoveToNext;
        StoreEvents = Wireup.Init().UsingInMemoryPersistence().Build();
        sut = new PollingClient2(StoreEvents.Advanced, c => HandleFunction(c));
    }

    protected override void Cleanup()
    {
        StoreEvents.Dispose();
        Sut.Dispose();
    }

    protected void WaitForCondition(Func<bool> predicate, int timeoutInSeconds = 4)
    {
        var startTest = DateTime.Now;
        while (!predicate() && DateTime.Now.Subtract(startTest).TotalSeconds < timeoutInSeconds) Thread.Sleep(100);
    }
}

#pragma warning restore IDE1006 // Naming Styles