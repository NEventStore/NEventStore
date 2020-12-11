namespace NEventStore.ConversionTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NEventStore.Conversion;
    using NEventStore.Persistence;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using FluentAssertions;
#if MSTEST
	using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NUNIT
    using NUnit.Framework;
#endif
#if XUNIT
	using Xunit;
	using Xunit.Should;
#endif

#if MSTEST
	[TestClass]
#endif
    public class when_opening_a_commit_that_does_not_have_convertible_events : using_event_converter
    {
        private ICommit _commit;

        private ICommit _converted;

        protected override void Context()
        {
            _commit = CreateCommit(new EventMessage { Body = new NonConvertingEvent() });
        }

        protected override void Because()
        {
            _converted = EventUpconverter.Select(_commit);
        }

        [Fact]
        public void should_not_be_converted()
        {
            _converted.Should().BeSameAs(_commit);
        }

        [Fact]
        public void should_have_the_same_instance_of_the_event()
        {
            _converted.Events.Single().Should().Be(_commit.Events.Single());
        }
    }

#if MSTEST
	[TestClass]
#endif
    public class when_opening_a_commit_that_has_convertible_events : using_event_converter
    {
        private ICommit _commit;

        private readonly Guid _id = Guid.NewGuid();
        private ICommit _converted;

        protected override void Context()
        {
            _commit = CreateCommit(new EventMessage { Body = new ConvertingEvent(_id) });
        }

        protected override void Because()
        {
            _converted = EventUpconverter.Select(_commit);
        }

        [Fact]
        public void should_be_of_the_converted_type()
        {
            _converted.Events.Single().Body.GetType().Should().Be(typeof(ConvertingEvent3));
        }

        [Fact]
        public void should_have_the_same_id_of_the_commited_event()
        {
            ((ConvertingEvent3)_converted.Events.Single().Body).Id.Should().Be(_id);
        }
    }

    // ReSharper disable InconsistentNaming
#if MSTEST
	[TestClass]
#endif
    public class when_an_event_converter_implements_the_IConvertEvents_interface_explicitly : using_event_converter
    // ReSharper restore InconsistentNaming
    {
        private ICommit _commit;
        private readonly Guid _id = Guid.NewGuid();
        private ICommit _converted;
        private EventMessage _eventMessage;

        protected override void Context()
        {
            _eventMessage = new EventMessage { Body = new ConvertingEvent2(_id, "FooEvent") };

            _commit = CreateCommit(_eventMessage);
        }

        protected override void Because()
        {
            _converted = EventUpconverter.Select(_commit);
        }

        [Fact]
        public void should_be_of_the_converted_type()
        {
            _converted.Events.Single().Body.GetType().Should().Be(typeof(ConvertingEvent3));
        }

        [Fact]
        public void should_have_the_same_id_of_the_commited_event()
        {
            ((ConvertingEvent3)_converted.Events.Single().Body).Id.Should().Be(_id);
        }
    }

    public abstract class using_event_converter : SpecificationBase
    {
        private IEnumerable<Assembly> _assemblies;
        private Dictionary<Type, Func<object, object>> _converters;
        private EventUpconverterPipelineHook _eventUpconverter;

        protected EventUpconverterPipelineHook EventUpconverter
        {
            get { return _eventUpconverter ?? (_eventUpconverter = CreateUpConverterHook()); }
        }

        private EventUpconverterPipelineHook CreateUpConverterHook()
        {
            _assemblies = GetAllAssemblies();
            _converters = GetConverters(_assemblies);
            return new EventUpconverterPipelineHook(_converters);
        }

        private Dictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
        {
            IEnumerable<KeyValuePair<Type, Func<object, object>>> c = from a in toScan
                                                                      from t in a.GetTypes()
                                                                      let i = t.GetInterface(typeof(IUpconvertEvents<,>).FullName)
                                                                      where i != null
                                                                      let sourceType = i.GetGenericArguments().First()
                                                                      let convertMethod = i.GetMethods(BindingFlags.Public | BindingFlags.Instance).First()
                                                                      let instance = Activator.CreateInstance(t)
                                                                      select new KeyValuePair<Type, Func<object, object>>(sourceType, e => convertMethod.Invoke(instance, new[] { e }));
            try
            {
                return c.ToDictionary(x => x.Key, x => x.Value);
            }
            catch (ArgumentException ex)
            {
                throw new MultipleConvertersFoundException(ex.Message, ex);
            }
        }

        private IEnumerable<Assembly> GetAllAssemblies()
        {
            return
                Assembly.GetCallingAssembly().GetReferencedAssemblies().Select(Assembly.Load).Concat(new[] { Assembly.GetCallingAssembly() });
        }

        protected static ICommit CreateCommit(EventMessage eventMessage)
        {
            return new Commit(Bucket.Default,
                Guid.NewGuid().ToString(),
                1,
                Guid.NewGuid(),
                1,
                DateTime.MinValue,
                1,
                null,
                new[] { eventMessage });
        }
    }

    public class ConvertingEventConverter : IUpconvertEvents<ConvertingEvent, ConvertingEvent2>
    {
        public ConvertingEvent2 Convert(ConvertingEvent sourceEvent)
        {
            return new ConvertingEvent2(sourceEvent.Id, "Temp");
        }
    }

    public class ExplicitConvertingEventConverter : IUpconvertEvents<ConvertingEvent2, ConvertingEvent3>
    {
        ConvertingEvent3 IUpconvertEvents<ConvertingEvent2, ConvertingEvent3>.Convert(ConvertingEvent2 sourceEvent)
        {
            return new ConvertingEvent3(sourceEvent.Id, "Temp", true);
        }
    }

    public class NonConvertingEvent
    { }

    public class ConvertingEvent
    {
        public ConvertingEvent(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class ConvertingEvent2
    {
        public ConvertingEvent2(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class ConvertingEvent3
    {
        public ConvertingEvent3(Guid id, string name, bool imExplicit)
        {
            Id = id;
            Name = name;
            ImExplicit = imExplicit;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool ImExplicit { get; set; }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169