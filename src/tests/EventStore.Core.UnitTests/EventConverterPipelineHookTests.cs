using System.Collections.Generic;

#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.UnitTests
{
    using System;
    using System.Linq;
    using Machine.Specifications;
    using Persistence;
    using It = Machine.Specifications.It;

    [Subject("EventConverterPipelineHook")]
    public class when_opening_a_commit_that_does_not_have_convertible_events
    {
        static readonly Commit commit = new Commit(
            Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null
        );
        static readonly EventConverterPipelineHook eventConverter = new EventConverterPipelineHook();
        static Commit converted;

        Establish context = () =>
            commit.Events.Add(new EventMessage() { Body = new NonConvertingEvent() });

        Because of = () => 
            converted = eventConverter.Select(commit);

        It should_not_be_converted = () =>
            converted.ShouldBeTheSameAs(commit);

        It should_have_the_same_instance_of_the_event = () =>
            converted.Events.Single().ShouldEqual(commit.Events.Single());
    }

    [Subject("EventConverterPipelineHook")]
    public class when_opening_a_commit_that_has_convertible_events
    {
        static readonly Commit commit = new Commit(
            Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null
        );
        static readonly EventConverterPipelineHook eventConverter = new EventConverterPipelineHook();
        static Commit converted;
        static Guid id = Guid.NewGuid();
        static readonly EventMessage eventMessage = new EventMessage {
            Body = new ConvertingEvent(id)
        };

        Establish context = () =>
            commit.Events.Add(eventMessage);

        Because of = () =>
            converted = eventConverter.Select(commit);

        It should_be_of_the_converted_type = () =>
            converted.Events.Single().Body.GetType().ShouldEqual(typeof(ConvertingEvent3));

        It should_have_the_same_id_of_the_commited_event = () =>
            ((ConvertingEvent3)converted.Events.Single().Body).Id.ShouldEqual(id);
    }

    [Subject("EventConverterPipelineHook")]
    public class when_an_event_converter_implements_the_IConvertEvents_interface_explicitly
    {
        static readonly Commit commit = new Commit(
            Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null
        );
        static readonly EventConverterPipelineHook eventConverter = new EventConverterPipelineHook();
        static Commit converted;
        static readonly Guid id = Guid.NewGuid();
        static readonly EventMessage eventMessage = new EventMessage
        {
            Body = new ConvertingEvent2(id, "FooEvent")
        };

        Establish context = () =>
            commit.Events.Add(eventMessage);

        Because of = () =>
            converted = eventConverter.Select(commit);

        It should_be_of_the_converted_type = () =>
            converted.Events.Single().Body.GetType().ShouldEqual(typeof(ConvertingEvent3));

        It should_have_the_same_id_of_the_commited_event = () =>
            ((ConvertingEvent3)converted.Events.Single().Body).Id.ShouldEqual(id);
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