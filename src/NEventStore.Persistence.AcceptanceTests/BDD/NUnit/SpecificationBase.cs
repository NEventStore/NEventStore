﻿#region

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

#endregion

#if NUNIT

namespace NEventStore.Persistence.AcceptanceTests.BDD.NUnit
{
    /// <summary>
    ///     base class for BDD testing in a Given-When-Then style
    ///     using NUnit
    ///     this will be used to implement a class that will test a single
    ///     action or behavior and multiple result conditions
    ///     - a class will represent a single scenario
    ///     - the class name will describe the scenario name
    /// </summary>
    [TestFixture]
    [LogSuite]
    [LogTest]
    public abstract class SpecificationBase
    {
        /// <summary>
        ///     there's a problem with error / exception reporting in here, testfixture setup is not well suited for
        ///     exception handling
        ///     worksround:
        ///     http://stackoverflow.com/questions/1411676/how-to-diagnose-testfixturesetup-failed
        ///     a good idea on how to catch and test for exceptions:
        ///     http://www.planetgeek.ch/2015/06/22/machine-specifications-the-alternative-nunit/
        ///     maybe catch the generated exception with something like: Catch.Exception() shown here and save it to a local
        ///     variable
        ///     in the when() function, then test for the exception in the 'then' tests
        /// </summary>
        [OneTimeSetUp]
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

        [SetUp]
        // NUnit doesn't support very useful logging of failures from a OneTimeSetUp method. We'll do the logging here.
        public void CheckForTestFixturefailure()
        {
            if (testFixtureSetupException != null)
            {
                var msg = string.Format(
                    "There was a failure during Context() or Because() phases.\n\rException: {0}\n\rStacktrace: {1}",
                    testFixtureSetupException.Message, testFixtureSetupException.StackTrace);
                Assert.Fail(msg);
            }
        }

        [OneTimeTearDown]
        protected virtual void Cleanup()
        {
        }

        private Exception testFixtureSetupException;

        protected virtual void Context()
        {
        }

        protected virtual void Because()
        {
        }
    }

    /// <summary>
    ///     Attribute used to identiy the tests
    ///     for custom actions:
    ///     http://nunit.org/index.php?p=actionAttributes&r=2.6.3
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ThenAttribute : TestAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class FactAttribute : TestAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class LogSuiteAttribute : Attribute, ITestAction
    {
        public void AfterTest(ITest test)
        {
            // Method intentionally left empty.
        }

        public void BeforeTest(ITest test)
        {
            Console.WriteLine("Scenario: {0}", test.Fixture.GetType().Name);
        }

        public ActionTargets Targets => ActionTargets.Suite;
    }

    /// <summary>
    ///     Attribute used to identify the tests
    ///     and describe them
    ///     for custom actions:
    ///     http://nunit.org/index.php?p=actionAttributes&r=2.6.3
    ///     http://nunit.org/index.php?p=testContext&r=2.6.3
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LogTestAttribute : Attribute, ITestAction
    {
        public void AfterTest(ITest test)
        {
            Console.WriteLine(" - {0} - {1}", test.Method.Name, TestContext.CurrentContext.Result.Outcome.Status);
        }

        public void BeforeTest(ITest test)
        {
            Console.WriteLine(test.Fixture.GetType().Name);
        }

        public ActionTargets Targets => ActionTargets.Test;
    }
}

#endif