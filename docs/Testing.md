# Testing And Test Frameworks in NEventStore

While upgrading the solution to support dotnet core, we also tried to migrate the tests to other test frameworks
(because not all of them supported dotnet core correctly when the migration job started).

Several trial and errors were made, but in the end we were able to implement the tests using all the 3 major testing frameworks available in the dotnet world:

- XUnit
- NUnit
- MSTest

We had to write 3 version of the `SpecificationBase` class and adapt the testing attributes to each framework.

I you inspect the code you'll see a lot of `#if NUNIT` (and the like) lines of code.

The actual implementation compiles all the projects to use NUnit.

You can change the behavior following these steps:

- go through all the .csproj files and change the compilation constant from NUNIT to XUNIT or MSTEST.
- in the assemblies that contain tests you need to reference the correct Test Framework assemblies and TestAdapter for the framework you are going to use.

Having more than one test runner might not be a good idea because some CI tools (like Appveyor) might autodetect them and execute the tests for a framework you are not using, and it will surely endup with failures and errors of your build.