NEventStore
===

NEventStore is a persistence library used to abstract different storage implementations
when using event sourcing as storage mechanism. This library is developed with a specific focus on [DDD](http://en.wikipedia.org/wiki/Domain-driven_design)/[CQRS](http://cqrsinfo.com) applications.

NEventStore currently supports:

- dotnet framework 4.5
- dotnet standard 2.0, dotnet core 2.0 

Build Status
===

Branches: 

- feature/dotnetcore [![Build status](https://ci.appveyor.com/api/projects/status/frg36pb2oh1j2ddi/branch/feature/dotnetcore?svg=true)](https://ci.appveyor.com/project/AGiorgetti/neventstore/branch/feature/dotnetcore)


Documentation
===

Please see the [documentation](https://github.com/NEventStore/NEventStore/wiki) to get started and for more information.

Version tracking can be [found here](Changelog.MD)

### Developed with:

[![Resharper](http://neventstore.org/images/logo_resharper_small.gif)](http://www.jetbrains.com/resharper/)
[![TeamCity](http://neventstore.org/images/logo_teamcity_small.gif)](http://www.jetbrains.com/teamcity/)
[![dotCover](http://neventstore.org/images/logo_dotcover_small.gif)](http://www.jetbrains.com/dotcover/)
[![dotTrace](http://neventstore.org/images/logo_dottrace_small.gif)](http://www.jetbrains.com/dottrace/)

# How to build

To build the project locally use the following scripts:

"RestorePackages.bat": let NuGet download all the packages it needs, you need to do this at least once to download all the tools needed to compile the library outside Visual Studio.

"Build.RunTask.bat TaskName": executes the specified Task, available tasks are:

- Clean - clean up the output and publish folders
- UpdateVersion - update the assembly version info files 
- Compile - compiles the solution
- Test - executes unit tests
- Build - executes Clean, UpdateVersion, Compile and Test 
- Package - executes Build and publishes the artifacts


