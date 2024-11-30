#if XUNIT
namespace NEventStore.Persistence.AcceptanceTests.BDD
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Xunit;
    using Xunit.Sdk;

    internal class SpecificationBaseRunner : ITestClassCommand
    {
        private readonly List<object> _fixtures = new List<object>();
        private SpecificationBase _objectUnderTest;

        public SpecificationBase ObjectUnderTest
        {
            get
            {
                if (_objectUnderTest == null)
                {
                    GuardTypeUnderTest();
                    _objectUnderTest = (SpecificationBase)Activator.CreateInstance(TypeUnderTest.Type);
                }

                return _objectUnderTest;
            }
        }

        object ITestClassCommand.ObjectUnderTest
        {
            get { return ObjectUnderTest; }
        }

        public ITypeInfo TypeUnderTest { get; set; }

        public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
        {
            return 0;
        }

        public Exception ClassStart()
        {
            try
            {
                SetupFixtures();
                ObjectUnderTest.OnStart();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public Exception ClassFinish()
        {
            try
            {
                ObjectUnderTest.OnFinish();

                foreach (var fixtureData in _fixtures)
                {
                    var disposable = fixtureData as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
        {
            string displayName = (TypeUnderTest.Type.Name + ", it " + testMethod.Name).Replace('_', ' ');
            return new[] { new SpecTestCommand(testMethod, displayName) };
        }

        public IEnumerable<IMethodInfo> EnumerateTestMethods()
        {
            GuardTypeUnderTest();

            return TypeUtility.GetTestMethods(TypeUnderTest);
        }

        public bool IsTestMethod(IMethodInfo testMethod)
        {
            return MethodUtility.IsTest(testMethod);
        }

        private void SetupFixtures()
        {
            try
            {
                foreach (var @interface in TypeUnderTest.Type.GetInterfaces())
                {
                    if (@interface.IsGenericType)
                    {
                        Type genericDefinition = @interface.GetGenericTypeDefinition();

                        if (genericDefinition == typeof(IUseFixture<>))
                        {
                            Type dataType = @interface.GetGenericArguments()[0];
                            if (dataType == TypeUnderTest.Type)
                            {
                                throw new InvalidOperationException("Cannot use a test class as its own fixture data");
                            }

                            object fixtureData = null;

                            fixtureData = Activator.CreateInstance(dataType);

                            MethodInfo method = @interface.GetMethod("SetFixture", new[] { dataType });
                            _fixtures.Add(fixtureData);
                            method.Invoke(ObjectUnderTest, new[] { fixtureData });
                        }
                    }
                }
            }
            catch (TargetInvocationException ex)
            {
                ExceptionUtility.RethrowWithNoStackTraceLoss(ex.InnerException);
            }
        }

        private void GuardTypeUnderTest()
        {
            if (TypeUnderTest == null)
            {
                throw new InvalidOperationException("Forgot to set TypeUnderTest before calling ObjectUnderTest");
            }

            if (!typeof(SpecificationBase).IsAssignableFrom(TypeUnderTest.Type))
            {
                throw new InvalidOperationException("SpecificationBaseRunner can only be used with types that derive from SpecificationBase");
            }
        }

        private class SpecTestCommand : TestCommand
        {
            public SpecTestCommand(IMethodInfo testMethod, string displayName) : base(testMethod, displayName, 0)
            {
            }

            public override MethodResult Execute(object testClass)
            {
                try
                {
                    testMethod.Invoke(testClass, null);
                }
                catch (ParameterCountMismatchException)
                {
                    throw new InvalidOperationException("Observation " + TypeName + "." + MethodName + " cannot have parameters");
                }

                return new PassedResult(testMethod, DisplayName);
            }
        }
    }
}

#endif