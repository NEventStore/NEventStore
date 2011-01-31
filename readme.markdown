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
_[Complete]_ Microsoft SQL Server 2000 (or later)  
_[Complete]_ MySQL 5.0 (or later)  
* _[Complete]_ InnoDB  
* _[Complete]_ NDB/MySQL Cluster  
* _[Complete]_ Drizzle  
* _[Complete]_ MariaDB  
* _[Complete]_ XtraDB  
* _[Complete]_ PBXT  
* _[Complete]_ Xeround  
* _[Complete]_ Galera  
* _[Complete]_ Percona  
* _[Complete]_ OurDelta  
* _[Untested]_ MyISAM  
* _[Untested]_ BerkleyDB  
_[Complete]_ PostgreSQL 8.0 (or later)  
_[Complete]_ Firebird 2.0 (or later)  
_[Planned]_ Oracle 8.0 (or later)  
_[Planned]_ IBM DB2  
_[Planned]_ Informix  
_[Planned]_ Sybase  

### Embedded Relational Databases
_[Complete]_ SQLite 3.0 (or later)  
_[Complete]_ Microsoft SQL Server Compact Edition 3.5 (or later)  
_[Complete]_ Microsoft Access 2000 (or later)  

### Cloud-based Relational Databases
_[Complete]_ Microsoft SQL Azure  
_[Complete]_ Amazon RDS  
_[In progress]_ Azure Tables  
_[In progress]_ Amazon SimpleDB  
_[Planned]_ Amazon S3  

### Document Databases
_[In progress]_ RavenDB r224 (or later)  
_[Planned]_ CouchDB 1.0 (or later)  
_[Beta]_ MongoDB 1.6 (or later)  

### File System
_[Planned]_ .NET Managed System.IO APIs _[Planned]_  

### Dynamo Clones
_[Planned]_ Cassandra  
_[Planned]_ Riak  
_[Planned]_ Voldemort  
_[Planned]_ Dynomite  

### KV Stores / NoSQL
_[Planned]_ HBase  
_[Planned]_ Redis  
_[Planned]_ Tokyo Cabinet  
_[Planned]_ Memcached (and variants)  
_[Planned]_ Hibari  
_[Planned]_ Keyspace  
_[Planned]_ OrientDB / OrientKV  
_[Planned]_ VoltDB  
_[Planned]_ BerkleyDB  
_[Planned]_ HampsterDB  

## Project Goals
* Mono 2.6 support  
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
For .NET v4.0, simply run build.cmd from the command line.  Users requiring a .NET v3.5 build may run build-net35.cmd.  
Once built, the files will be placed in the "output" subdirectory.

## Using the EventStore
Please see [EventStore.Example](https://github.com/joliver/EventStore/blob/master/doc/EventStore.Example/ExampleUsage.cs) project in the doc subdirectory.