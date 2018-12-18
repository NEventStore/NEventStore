#if MSTEST

namespace NEventStore.Persistence.AcceptanceTests.BDD
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using System;

	/// <summary>
	/// base class for BDD testing in a Given-When-Then style
	/// using MSTest
	/// 
	/// in MSTest each test will be executed by a new instance of the test class.
	/// 
	/// this will be used to implement a class that will test a single 
	/// action or behavior and multiple result conditions
	/// 
	/// - a class will represent a single scenario
	/// - the class name will describe the scenario name
	/// </summary>
	[TestClass]
	public abstract class SpecificationBase
	{
		Exception testFixtureSetupException = null;

		/// <summary>
		/// there's a problem with error / exception reporting in here, testfixture setup is not well suited for 
		/// exception handling
		/// worksround:
		/// http://stackoverflow.com/questions/1411676/how-to-diagnose-testfixturesetup-failed
		/// 
		/// a good idea on how to catch and test for exceptions:
		/// http://www.planetgeek.ch/2015/06/22/machine-specifications-the-alternative-nunit/
		/// 
		/// maybe catch the generated exception with something like: Catch.Exception() shown here and save it to a local variable
		/// in the when() function, then test for the exception in the 'then' tests 
		/// </summary>
		[TestInitialize] // I cannot have something like a OnTimeTestInitialize in MsTest, ClassInitialize requires static methods
		public void SetUp()
		{
			try
			{
				Context();
				Because();
			}
			catch (Exception ex)
			{
				testFixtureSetupException = ex;
			}

		}

		[TestInitialize]
		// NUnit doesn't support very useful logging of failures from a OneTimeSetUp method. We'll do the logging here.
		public void CheckForTestFixturefailure()
		{
			if (testFixtureSetupException != null)
			{
				string msg = string.Format("There was a failure during Context() or Because() phases.\n\rException: {0}\n\rStacktrace: {1}",
					testFixtureSetupException.Message, testFixtureSetupException.StackTrace);
				Assert.Fail(msg);
			}
		}

		protected virtual void Context() { }
		protected virtual void Because() { }


		[ClassCleanup]
		protected virtual void Cleanup() { }
	}

	/// <summary>
	/// Attribute used to identiy the tests
	/// 
	/// for custom actions:
	/// http://nunit.org/index.php?p=actionAttributes&r=2.6.3
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ThenAttribute : LogTestMethod
	{ }

	[AttributeUsage(AttributeTargets.Method)]
	public class FactAttribute : LogTestMethod
	{ }

	public class LogTestMethod : TestMethodAttribute
	{
		public override TestResult[] Execute(ITestMethod testMethod)
		{
			Console.WriteLine("Scenario: {0}", testMethod.TestClassName);

			var result = base.Execute(testMethod);

			Console.WriteLine(" - {0} - {1}", testMethod.TestMethodName, result[0].Outcome == UnitTestOutcome.Passed);

			return result;
		}
	}

}

#endif