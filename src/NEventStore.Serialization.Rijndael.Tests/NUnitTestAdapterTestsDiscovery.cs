#region

using NUnit.Framework;

#endregion

namespace NEventStore.Serialization.Rijndael.Tests;

/// <summary>
///     this is needed to allow NUnit test adapter to discover the tests,
///     if we do not have any class explicitly marked with the TestFixture attribute
///     all the assembly will be ignored (even if some class inherit from something which is
///     marked with the attribute in a reference assembly)
/// </summary>
#if NUNIT
[TestFixture]
public class NUnitTestAdapterTestsDiscovery
{
}
#endif