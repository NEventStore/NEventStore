NEventStore
======================================================================

 - The most recent stable release is avaiable on [NuGet.org](https://nuget.org/packages/NEventStore)
 - CI builds can be viewed on [Codebetter's TeamCity server](http://teamcity.codebetter.com/project.html?projectId=project247&tab=projectOverview).
 - [CI package feed] (https://www.myget.org/F/neventstore-ci/)

## Overview
NEventStore is a persistence library used to abstract different storage implementations
when using event sourcing as storage mechanism.  Event sourcing is most closely associated
with a concept known as [CQRS](http://cqrsinfo.com).

### Need Help? Have a Question?
Ask your question on [Stack Overflow](http://stackoverflow.com/search?q=[cqrs]+neventstore) and tag your question with
the CQRS tag and the word "NEventStore" in the title.

### Purpose and Theory
The purpose of an event store is to represent a series of events as a stream.  Furthermore,
it provides hooks whereby any events committed to the stream can be dispatched to interested
parties.

Guided by a number strategic design decisions based upon the needs of applications using event sourcing,
NEventStore is able to liberate applications from the stringent requirements often imposed by
infrastructure components.  Specifically, most CQRS-style applications read from a message queue
and perform some processing.  When processing is complete, the application then commits the work
to storage and publishes the completed work.  In almost all cases, this requires a two-phase commit
managed by a distributed transaction coordinator (MSDTC in .NET) along with various security settings
and firewall ports opened and avaiable whereby such components can communicate, not to mention a
ubiquitous requirement for Microsoft Windows on all machines in .NET environments.

When using two-phase commit in .NET, there are very few database drivers that support this scenario
and even fewer message queues that support it.  In essence, if you want to implement a typical
CQRS-style application, you're stuck with MSMQ and SQL Server using MSDTC.  Granted, there are
other choices, but the constraints imposed by a two-phase commit are burdensome.  This also
creates additional issues when utilizing shared hosting or running on Mono as support in frameworks
and drivers is either poor, buggy, or unavailable.

NEventStore liberates application developers from this level of infrastructure awareness and
concern by committing all work within a separate isolated atomic unit--all without using transactions.
Furthermore, it does this outside of any ambient transaction from a message queue or other
persistence mechanisms.  In other words, application developers are free to use virtually any
messaging queuing infrastructure, message bus (if at all), and storage engine.  Each will perform
its own specific task in an isolated manner with full transactional integrity all without
enlisting any resources (other than a message queue) in some form of transaction.

Interestingly enough, even without the presence of distributed transactions across the various resources
involved, such as a message queue and persistent storage, the EventStore is able to ensure a fully
transactional experience.  This is achieved by breaking apart a distributed transaction into smaller
pieces and performing each one individually.  This is one of the primary goals and motivations in the
underlying model found in the EventStore.  Thus each message delivered by the queuing infrastructure is
made to be idempotent, even though the message may be delivered multiple times, as per message queue
"at-least-once" guarantees.  Following this, the EventStore is able to ensure that all events committed
are always dispatched to any messaging infrastructure.

**New in v3.0:** I knew you guys couldn't live without it, so for those storage engines and message systems
which support and participate two-phase commits, there now exists the ability to specify a
TransactionScopeOption of 'Required'.  Simply indicate this using the following wireup overload:

	var store = Wireup.Init()
		.UsingSqlPersistence("connection-name-here") // also works with UsingRavenPersistence()
			.EnlistInAmbientTransaction()

## Supported Storage Engines

See [supported persistence engines](https://github.com/NEventStore/NEventStore/wiki/Supported-Persistence-Engines) in wiki.

## Project Goals
* Mono 2.4 support  
* Medium-trust support  
* Support more storage engines than any other event storage implementation  
* Easily support virtually any storage engine (NoSQL, etc.)  
* Avoid dependence upon TransactionScope or Transactions while maintaining full data integrity  
* Full test coverage of storage implementations  
* Easily hook into any bus implementation (NServiceBus, MassTransit, etc.)  
* Synchronous and asynchronous dispatching of events  
* Extreme performance  
* Multi-thread safe  
* Fluent builder

## Building
To build the solution and execute all tests, run **Build.RunTask.bat** from the command line.

If you would also like to package the solution for distribution, pass in the 'package' command by running **Build.RunTask.bat package** from the command line. Once built, the files will be placed in the "publish-net40" subdirectory.

*Note:* You may need to run the build with Administrator rights the first time. This is because NEventStore will attempt to create several performance counters and that action requires admin rights. If you would prefer to manually create the performance counters yourself, refer to NEventStore.Diagnostics.PerformanceCounters. 

## Using NEventStore

	var store = Wireup.Init()
		.UsingSqlPersistence("Name Of EventStore ConnectionString In Config File")
			.InitializeStorageEngine()
			.UsingJsonSerialization()
				.Compress()
				.EncryptWith(EncryptionKey)
		.HookIntoPipelineUsing(new[] { new AuthorizationPipelineHook() })
		.UsingAsynchronousDispatchScheduler()
			// Example of NServiceBus dispatcher: https://gist.github.com/1311195
			.DispatchTo(new My_NServiceBus_Or_MassTransit_OrEven_WCF_Adapter_Code())
		.Build();		

	/* NOTE: This following is merely *example* code. */

	using (store)
	{
		// some business code here
		using (var stream = store.CreateStream(myMessage.CustomerId))
		{
			stream.Add(new EventMessage { Body = myMessage });
			stream.CommitChanges(myMessage.MessageId);
		}
		
		using (var stream = store.OpenStream(myMessage.CustomerId, 0, int.MaxValue))
		{
			foreach (var @event in stream.CommittedEvents)
			{
				// business processing...
			}
		}
	}

For a more complete example, please see [NEventStore.Example](https://github.com/NEventStore/NEventStore/blob/master/src/NEventStore.Example/MainProgram.cs) project in the doc subdirectory.

## Running the Example
The NEventStore.Example project is configured by default to use a SQL event store. To run the example 
program, either change the SQL connection string in the app.config file to connect to a existing SQL database 
or change WireupEventStore() to call UsingInMemoryPersistence() rather than UsingSqlPersistence().
