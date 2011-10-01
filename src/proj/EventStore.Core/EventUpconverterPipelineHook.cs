namespace EventStore
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class EventUpconverterPipelineHook : IPipelineHook
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterPipelineHook));
		private readonly Dictionary<Type, Func<object, object>> converters;

		public EventUpconverterPipelineHook(Dictionary<Type, Func<object, object>> converters)
		{
			this.converters = converters;
		}

		public virtual Commit Select(Commit committed)
		{
			foreach (var eventMessage in committed.Events)
			{
				eventMessage.Body = this.Convert(eventMessage.Body);
			}
			return committed;
		}
		private object Convert(object body)
		{
			Func<object, object> converter;
			var result = body;
			if (this.converters.TryGetValue(body.GetType(), out converter))
			{
				result = this.Convert(converter(body));
				Logger.Debug(Resources.ConvertingEvent, body.GetType(), result.GetType());
			}
			return result;
		}

		public virtual bool PreCommit(Commit attempt)
		{
			return true;
		}

		public virtual void PostCommit(Commit committed)
		{
		}

		public void Dispose()
		{
			this.converters.Clear();
			GC.SuppressFinalize(this);
		}
	}
}
