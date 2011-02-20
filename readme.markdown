EventStore v2.0
======================================================================

## Overview
The EventStore is a persistence library used to abstract different storage implementations
when using event sourcing as storage mechanism.  Event sourcing is most closely associated
with a concept known as [CQRS](http://cqrsinfo.com).

### Purpose and Theory
The purpose of the EventStore is to represent a series of events as a stream.  Furthermore,
it provides hooks whereby any events committed to the stream can be dispatched to interested
parties.

Guided by a number strategic design decisions based upon the needs of applications using event sourcing,
the EventStore is able to liberate applications from the stringent requirements often imposed by
infrastructure components.  Specifically, most CQRS-style applications read from a message queue
and perform some processing.  When processing is complete, the application then commits the work
to storage and publishes the completed work.  In almost all cases, this requires a two-phase commit
managed by a distributed transaction coordinator (MSDTC in .NET) along with various security settings
and ports available whereby such components can communicate.

When using a two-phase commit in .NET, there are very few database drivers that support this scenario
and even fewer message queues that support it as well.  In essence, if you want to implement a typical
CQRS-style application, you're stuck with MSMQ and SQL Server using MSDTC.  Granted, there are
other choices, but the constraints imposed by a two-phase commit are burdensome.  This also
creates additional issues when utilizing shared hosting or running on Mono as support in frameworks
and drivers is either poor, buggy, or unavailable.

The EventStore liberates application developers from this level of infrastructure awareness and
concern by committing all work within a separate isolated atomic unit--all without using transactions.
Furthermore, it does this outside of any ambient transaction from a message queue or other
persistence mechanisms.  In other words, application developers are free to use virtually any
messaging queuing infrastructure, message bus (if at all), and storage engine.  Each will perform
its own specific task in an isolated manner with full transactional integrity all without
enlisting any resources (other than a message queue) in some form of a transaction.

## Supported Storage Engines

### Relational Databases
[Complete] Microsoft SQL Server 2000 (or later)  
[Complete] MySQL 5.0 (or later)  
* [Complete] InnoDB  
* [Complete] NDB/MySQL Cluster  
* [Complete] Drizzle  
* [Complete] MariaDB  
* [Complete] XtraDB  
* [Complete] PBXT  
* [Complete] Xeround  
* [Complete] Galera  
* [Complete] Percona  
* [Complete] OurDelta  
* [Untested] MyISAM  
* [Untested] BerkleyDB  
[Complete] PostgreSQL 8.0 (or later)  
[Complete] Firebird 2.0 (or later)  
[Planned] Oracle 8.0 (or later)  
[Planned] IBM DB2  
[Planned] Informix  
[Planned] Sybase  

### Embedded Relational Databases
[Complete] SQLite 3.0 (or later)  
[Complete] Microsoft SQL Server Compact Edition 3.5 (or later)  
[Complete] Microsoft Access 2000 (or later)  

### Cloud-based Relational Databases
[Complete] Microsoft SQL Azure  
[Complete] Amazon RDS  
[In progress] Azure Tables  
[In progress] Amazon SimpleDB  
[Planned] Amazon S3  

### Document Databases
[Complete] RavenDB r264 (or later)  
[Complete] MongoDB 1.6 (or later)  
[Planned] CouchDB 1.0 (or later)  

### File System
[Planned] .NET Managed System.IO APIs    

### Dynamo Clones
[Planned] Cassandra  
[Planned] Riak  
[Planned] Voldemort  
[Planned] Dynomite  

### KV Stores / NoSQL
[Planned] HBase  
[Planned] Redis  
[Planned] Tokyo Cabinet  
[Planned] Memcached
[Planned] Membase
[Planned] Microsoft Velocity
[Planned] SharedCache
[Planned] Hibari  
[Planned] Keyspace  
[Planned] OrientDB / OrientKV  
[Planned] VoltDB  
[Planned] BerkleyDB  
[Planned] HampsterDB  

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
For .NET v4.0, simply run **build.cmd** from the command line.  Users requiring a .NET v3.5 build may run **build-net35.cmd**.  
Once built, the files will be placed in the "output" subdirectory.

## Using the EventStore
Please see [EventStore.Example](https://github.com/joliver/EventStore/blob/master/doc/EventStore.Example/ExampleUsage.cs) project in the doc subdirectory.