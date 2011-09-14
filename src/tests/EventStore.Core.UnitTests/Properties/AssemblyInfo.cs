using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("EventStore.Core.UnitTests")]
[assembly: AssemblyDescription("")]
[assembly: Guid("c866499a-e56f-436d-a58f-2de993c0fa09")]

[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames",
	Justification = "Machine.Specifications is not signed, therefore this assembly cannot be signed.")]