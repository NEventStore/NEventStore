using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("EventStore.Serialization.ServiceStack")]
[assembly: AssemblyDescription("")]
[assembly: Guid("1fcca9eb-2abc-4cf3-bb59-cad2f0485782")]

[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames",
	Justification = "ServiceStack.Text is not signed, therefore this assembly cannot be signed.")]