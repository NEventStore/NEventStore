#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
    using System;
    using System.Linq;
    using Machine.Specifications;
    using System.Collections.Generic;
    using System.Reflection;
    using It = Machine.Specifications.It;

    [Subject("EventUpconverterPipelineHook")]
    public class when_opening_a_commit_that_does_not_have_convertible_events : using_event_converter
    {
        static readonly Commit commit = new Commit(
            Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null
        );
        static Commit converted;

        Establish context = () =>
            commit.Events.Add(new EventMessage { Body = new NonConvertingEvent() });

        Because of = () => 
            converted = EventUpconverter.Select(commit);

        It should_not_be_converted = () =>
            converted.ShouldBeTheSameAs(commit);

        It should_have_the_same_instance_of_the_event = () =>
            converted.Events.Single().ShouldEqual(commit.Events.Single());
    }

    [Subject("EventUpconverterPipelineHook")]
    public class when_opening_a_commit_that_has_convertible_events : using_event_converter
    {
        static readonly Commit commit = new Commit(
            Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null
        );
        static Commit converted;
        static Guid id = Guid.NewGuid();
        static readonly EventMessage eventMessage = new EventMessage {
            Body = new ConvertingEvent(id)
        };

        Establish context = () =>
            commit.Events.Add(eventMessage);

        Because of = () =>
            converted = EventUpconverter.Select(commit);

        It should_be_of_the_converted_type = () =>
            converted.Events.Single().Body.GetType().ShouldEqual(typeof(ConvertingEvent3));

        It should_have_the_same_id_of_the_commited_event = () =>
            ((ConvertingEvent3)converted.Events.Single().Body).Id.ShouldEqual(id);
    }

    [Subject("EventUpconverterPipelineHook")]
    public class when_an_event_converter_implements_the_IConvertEvents_interface_explicitly : using_event_converter
    {
        static readonly Commit commit = new Commit(
            Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null
        );
        static Commit converted;
        static readonly Guid id = Guid.NewGuid();
        static readonly EventMessage eventMessage = new EventMessage
        {
            Body = new ConvertingEvent2(id, "FooEvent")
        };

        Establish context = () =>
            commit.Events.Add(eventMessage);

        Because of = () =>
            converted = EventUpconverter.Select(commit);

        It should_be_of_the_converted_type = () =>
            converted.Events.Single().Body.GetType().ShouldEqual(typeof(ConvertingEvent3));

        It should_have_the_same_id_of_the_commited_event = () =>
            ((ConvertingEvent3)converted.Events.Single().Body).Id.ShouldEqual(id);
    }

    public class using_event_converter
    {
        protected static IEnumerable<Assembly> assemblies;
        protected static Dictionary<Type, Func<object, object>> converters;
        protected static EventUpconverterPipelineHook EventUpconverter;

        Establish context = () => {
            assemblies = getAllAssemblies();
            converters = GetConverters(assemblies);
            EventUpconverter = new EventUpconverterPipelineHook(converters);
        };

        private static Dictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
        {
            var c = from a in toScan
                    from t in a.GetTypes()
                    let i = t.GetInterface(typeof(IConvertEvents<,>).FullName)
                    where i != null
                    let sourceType = i.GetGenericArguments().First()
                    let convertMethod = i.GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    let instance = Activator.CreateInstance(t)
                    select new KeyValuePair<Type, Func<object, object>>(
                        sourceType,
                        e => convertMethod.Invoke(instance, new[] { e as object })
                    );
            return c.ToDictionary(x => x.Key, x => x.Value);
        }

        private static IEnumerable<Assembly> getAllAssemblies()
        {
            return Assembly.GetCallingAssembly()
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Concat(new[] {Assembly.GetCallingAssembly()});
        }
    }

    class ConvertingEventConverter : IConvertEvents<ConvertingEvent, ConvertingEvent2>
    {
        public ConvertingEvent2 Convert(ConvertingEvent sourceEvent)
        {
            return new ConvertingEvent2(sourceEvent.Id, "Temp");
        }
    }

    class ExplicitConvertingEventConverter : IConvertEvents<ConvertingEvent2, ConvertingEvent3>
    {
        ConvertingEvent3 IConvertEvents<ConvertingEvent2, ConvertingEvent3>.Convert(ConvertingEvent2 sourceEvent)
        {
            return new ConvertingEvent3(sourceEvent.Id, "Temp", true);
        }
    }

    class NonConvertingEvent {}
    class ConvertingEvent
    {
        public Guid Id { get; set; }

        public ConvertingEvent(Guid id)
        {
            Id = id;
        }
    }
    class ConvertingEvent2
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public ConvertingEvent2(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
    class ConvertingEvent3
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool ImExplicit { get; set; }

        public ConvertingEvent3(Guid id, string name, bool imExplicit)
        {
            Id = id;
            Name = name;
            ImExplicit = imExplicit;
        }
    }
}

// ReSharper enable InconsistentNaming
#pragma warning restore 169