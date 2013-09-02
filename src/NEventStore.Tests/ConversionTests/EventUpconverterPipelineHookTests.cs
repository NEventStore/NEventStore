
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace NEventStore.ConversionTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NEventStore.Conversion;
    using NEventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;

    public class when_opening_a_commit_that_does_not_have_convertible_events : using_event_converter
    {
        private readonly ICommit _commit = new Commit(Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);

        private ICommit _converted;

        protected override void Context()
        {
            _commit.Events.Add(new EventMessage {Body = new NonConvertingEvent()});
        }

        protected override void Because()
        {
            _converted = EventUpconverter.Select(_commit);
        }

        [Fact]
        public void should_not_be_converted()
        {
            _converted.ShouldBeSameAs(_commit);
        }

        [Fact]
        public void should_have_the_same_instance_of_the_event()
        {
            _converted.Events.Single().ShouldBe(_commit.Events.Single());
        }
    }

    public class when_opening_a_commit_that_has_convertible_events : using_event_converter
    {
        private readonly ICommit _commit = new Commit(Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);

        private readonly Guid _id = Guid.NewGuid();
        private ICommit _converted;
        private EventMessage _eventMessage;

        protected override void Context()
        {
            _eventMessage = new EventMessage {Body = new ConvertingEvent(_id)};

            _commit.Events.Add(_eventMessage);
        }

        protected override void Because()
        {
            _converted = EventUpconverter.Select(_commit);
        }

        [Fact]
        public void should_be_of_the_converted_type()
        {
            _converted.Events.Single().Body.GetType().ShouldBe(typeof (ConvertingEvent3));
        }

        [Fact]
        public void should_have_the_same_id_of_the_commited_event()
        {
            ((ConvertingEvent3) _converted.Events.Single().Body).Id.ShouldBe(_id);
        }
    }

    public class when_an_event_converter_implements_the_IConvertEvents_interface_explicitly : using_event_converter
    {
        private readonly ICommit _commit = new Commit(Guid.NewGuid().ToString(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);

        private readonly Guid _id = Guid.NewGuid();
        private ICommit _converted;
        private IEventMessage _eventMessage;

        protected override void Context()
        {
            _eventMessage = new EventMessage {Body = new ConvertingEvent2(_id, "FooEvent")};

            _commit.Events.Add(_eventMessage);
        }

        protected override void Because()
        {
            _converted = EventUpconverter.Select(_commit);
        }

        [Fact]
        public void should_be_of_the_converted_type()
        {
            _converted.Events.Single().Body.GetType().ShouldBe(typeof (ConvertingEvent3));
        }

        [Fact]
        public void should_have_the_same_id_of_the_commited_event()
        {
            ((ConvertingEvent3) _converted.Events.Single().Body).Id.ShouldBe(_id);
        }
    }

    public class using_event_converter : SpecificationBase
    {
        private IEnumerable<Assembly> assemblies;
        private Dictionary<Type, Func<object, object>> converters;
        private EventUpconverterPipelineHook eventUpconverter;

        protected EventUpconverterPipelineHook EventUpconverter
        {
            get { return eventUpconverter ?? (eventUpconverter = CreateUpConverterHook()); }
        }

        private EventUpconverterPipelineHook CreateUpConverterHook()
        {
            assemblies = GetAllAssemblies();
            converters = GetConverters(assemblies);
            return new EventUpconverterPipelineHook(converters);
        }

        private Dictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
        {
            IEnumerable<KeyValuePair<Type, Func<object, object>>> c = from a in toScan
                from t in a.GetTypes()
                let i = t.GetInterface(typeof (IUpconvertEvents<,>).FullName)
                where i != null
                let sourceType = i.GetGenericArguments().First()
                let convertMethod = i.GetMethods(BindingFlags.Public | BindingFlags.Instance).First()
                let instance = Activator.CreateInstance(t)
                select new KeyValuePair<Type, Func<object, object>>(sourceType, e => convertMethod.Invoke(instance, new[] {e}));
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
                Assembly.GetCallingAssembly().GetReferencedAssemblies().Select(Assembly.Load).Concat(new[] {Assembly.GetCallingAssembly()});
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
    {}

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