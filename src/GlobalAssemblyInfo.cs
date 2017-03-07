//-----------------------------------------------------------------------
// <copyright file="GlobalAssemblyInfo.cs">
//	 Copyright (c) Jonathan Oliver. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !NETSTANDARD1_6
[assembly: AssemblyCompany("NEventStore")]
[assembly: AssemblyProduct("NEventStore")]
[assembly: AssemblyCopyright("Copyright � Jonathan Oliver, Jonathan Mathius, Damian Hickey and Contributors 2011-2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguage("en-US")]
#endif
[assembly: InternalsVisibleTo("NEventStore.Tests")]
[assembly: InternalsVisibleTo("NEventStore.Core.Tests")]
