
#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace EventStore.Core.Tests.ConversionTests
{
    using EventStore.Persistence.AcceptanceTests.BDD;
    using Xunit;
    using Xunit.Should;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using EventStore.Conversion;

    public class when_opening_a_commit_that_does_not_have_convertible_events : using_event_converter
	{
		readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		Commit converted;

	    protected override void Context()
	    {
	        commit.Events.Add(new EventMessage {Body = new NonConvertingEvent()});
	    }

	    protected override void Because()
	    {
	        converted = EventUpconverter.Select(commit);
	    }

	    public void should_not_be_converted()
	    {
	        converted.ShouldBeSameAs(commit);
	    }

        [Fact]
	    public void should_have_the_same_instance_of_the_event()
	    {
	        converted.Events.Single().ShouldBe(commit.Events.Single());
	    }
	}

	public class when_opening_a_commit_that_has_convertible_events : using_event_converter
	{
		readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		readonly Guid id = Guid.NewGuid();
		EventMessage eventMessage;
		Commit converted;

	    protected override void Context()
	    {
	        eventMessage = new EventMessage
	        {
	            Body = new ConvertingEvent(id)
	        };

	        commit.Events.Add(eventMessage);
	    }

	    protected override void Because()
	    {
	        converted = EventUpconverter.Select(commit);
	    }

        [Fact]
        public void should_be_of_the_converted_type()
	    {
	        converted.Events.Single().Body.GetType().ShouldBe(typeof (ConvertingEvent3));
	    }

        [Fact]
        public void should_have_the_same_id_of_the_commited_event()
	    {
	        ((ConvertingEvent3) converted.Events.Single().Body).Id.ShouldBe(id);
	    }
	}

	public class when_an_event_converter_implements_the_IConvertEvents_interface_explicitly : using_event_converter
	{
		readonly Commit commit = new Commit(
			Guid.NewGuid(), 0, Guid.NewGuid(), 0, DateTime.MinValue, null, null);
		readonly Guid id = Guid.NewGuid();
		EventMessage eventMessage;
		Commit converted;

	    protected override void Context()
	    {
            eventMessage = new EventMessage
            {
                Body = new ConvertingEvent2(id, "FooEvent")
            };

	        commit.Events.Add(eventMessage);
	    }

	    protected override void Because()
	    {
	        converted = EventUpconverter.Select(commit);
	    }

        [Fact]
        public void should_be_of_the_converted_type()
	    {
	        converted.Events.Single().Body.GetType().ShouldBe(typeof (ConvertingEvent3));
	    }

        [Fact]
        public void should_have_the_same_id_of_the_commited_event()
	    {
	        ((ConvertingEvent3) converted.Events.Single().Body).Id.ShouldBe(id);
	    }
	}

	public class using_event_converter : SpecificationBase
	{
		protected IEnumerable<Assembly> assemblies;
		protected Dictionary<Type, Func<object, object>> converters;
	    EventUpconverterPipelineHook eventUpconverter;

	    public EventUpconverterPipelineHook EventUpconverter
	    {
            get { return eventUpconverter ?? (eventUpconverter = CreateUpConverterHook()); }
	    }
        
	    EventUpconverterPipelineHook CreateUpConverterHook()
	    {
	        assemblies = GetAllAssemblies();
	        converters = GetConverters(assemblies);
	        return new EventUpconverterPipelineHook(converters);
	    }

	    private Dictionary<Type, Func<object, object>> GetConverters(IEnumerable<Assembly> toScan)
		{
			var c = from a in toScan
					from t in a.GetTypes()
					let i = t.GetInterface(typeof(IUpconvertEvents<,>).FullName)
					where i != null
					let sourceType = i.GetGenericArguments().First()
					let convertMethod = i.GetMethods(BindingFlags.Public | BindingFlags.Instance).First()
					let instance = Activator.CreateInstance(t)
					select new KeyValuePair<Type, Func<object, object>>(
						sourceType,
						e => convertMethod.Invoke(instance, new[] { e }));
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
			return Assembly.GetCallingAssembly()
				.GetReferencedAssemblies()
				.Select(Assembly.Load)
				.Concat(new[] { Assembly.GetCallingAssembly() });
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
	{
	}
	public class ConvertingEvent
	{
		public Guid Id { get; set; }
		public ConvertingEvent(Guid id)
		{
			Id = id;
		}
	}
	public class ConvertingEvent2
	{
		public Guid Id { get; set; }
		public string Name { get; set; }

		public ConvertingEvent2(Guid id, string name)
		{
			Id = id;
			Name = name;
		}
	}
	public class ConvertingEvent3
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