using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace EventStore.Persistence.AcceptanceTests.BDD
{
    class SpecificationBaseRunner : ITestClassCommand
    {
        SpecificationBase objectUnderTest;
        readonly List<object> fixtures = new List<object>();

        public SpecificationBase ObjectUnderTest
        {
            get
            {
                if (objectUnderTest == null)
                {
                    GuardTypeUnderTest();
                    objectUnderTest = (SpecificationBase)Activator.CreateInstance(TypeUnderTest.Type);
                }

                return objectUnderTest;
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

                foreach (object fixtureData in fixtures)
                {
                    var disposable = fixtureData as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
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

        void SetupFixtures()
        {
            try
            {
                foreach (Type @interface in TypeUnderTest.Type.GetInterfaces())
                {
                    if (@interface.IsGenericType)
                    {
                        var genericDefinition = @interface.GetGenericTypeDefinition();

                        if (genericDefinition == typeof(IUseFixture<>))
                        {
                            var dataType = @interface.GetGenericArguments()[0];
                            if (dataType == TypeUnderTest.Type)
                                throw new InvalidOperationException("Cannot use a test class as its own fixture data");

                            object fixtureData = null;

                            fixtureData = Activator.CreateInstance(dataType);

                            var method = @interface.GetMethod("SetFixture", new Type[] { dataType });
                            fixtures.Add(fixtureData);
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

        void GuardTypeUnderTest()
        {
            if (TypeUnderTest == null)
                throw new InvalidOperationException("Forgot to set TypeUnderTest before calling ObjectUnderTest");

            if (!typeof(SpecificationBase).IsAssignableFrom(TypeUnderTest.Type))
                throw new InvalidOperationException("SpecificationBaseRunner can only be used with types that derive from SpecificationBase");
        }
        
        class SpecTestCommand : TestCommand
        {
            public SpecTestCommand(IMethodInfo testMethod, string displayName)
                : base(testMethod, displayName, 0) { }

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