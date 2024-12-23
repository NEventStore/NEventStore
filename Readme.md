NEventStore
===

NEventStore is a persistence library used to abstract different storage implementations when using event sourcing as storage mechanism. 

This library is developed with a specific focus on [DDD](http://en.wikipedia.org/wiki/Domain-driven_design)/[CQRS](https://en.wikipedia.org/wiki/Command%E2%80%93query_separation#Command_query_responsibility_segregation) applications.

NEventStore currently supports:

- .net standard 2.0
- .net framework 4.6.2

Starting from Version 6.0.0 NEventStore will use [Semantic Versioning](https://semver.org/) to track the version numbers.

Build Status (AppVeyor)
===

Branches: 

- master [![Build status](https://ci.appveyor.com/api/projects/status/frg36pb2oh1j2ddi/branch/master?svg=true)](https://ci.appveyor.com/project/AGiorgetti/neventstore/branch/master)
- develop [![Build status](https://ci.appveyor.com/api/projects/status/frg36pb2oh1j2ddi/branch/develop?svg=true)](https://ci.appveyor.com/project/AGiorgetti/neventstore/branch/develop)

Main Library Packages
===

- NEventStore - the core library package.
- NEventStore.Serialization.Json - Json serialization to be used with an IDocumentSerializer.
- NEventStore.Serialization.Bson - BSon serialization to be used with an IDocumentSerializer.
- NEventStore.Serialization.MsgPack - Message Pack serialization to be used with an IDocumentSerializer.
- NEventStore.PollingClient - provides an implementation for a PollingClient.

Documentation
===

Please see the [documentation](https://github.com/NEventStore/NEventStore/wiki) to get started and for more information.

ChangeLog can be [found here](https://github.com/NEventStore/NEventStore/blob/master/Changelog.md)

### Developed with:

[![Resharper](http://neventstore.org/images/logo_resharper_small.gif)](http://www.jetbrains.com/resharper/)
[![TeamCity](http://neventstore.org/images/logo_teamcity_small.gif)](http://www.jetbrains.com/teamcity/)
[![dotCover](http://neventstore.org/images/logo_dotcover_small.gif)](http://www.jetbrains.com/dotcover/)
[![dotTrace](http://neventstore.org/images/logo_dottrace_small.gif)](http://www.jetbrains.com/dottrace/)

# How to build (Windows OS)

To build the project locally on a Windows Machine:

- Open a Powershell console in Administrative mode and run the build script `build.ps1` in the root of the repository.

## Versioning

Versioning is done automatically by the build script updating the
AssemblyInfo.cs file (<GenerateAssemblyInfo>false</GenerateAssemblyInfo> in .csproj files) 
before the build starts. The version number is retrieved
from the git repository tags using "gitversion" tool.

Things are handled this way because NEventStore is used a submodule in other projects and it
need to have it's own version number when building other projects.

You should not update the version number manually, not commit the updated AssemblyInfo files.