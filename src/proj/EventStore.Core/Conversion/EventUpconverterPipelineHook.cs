namespace EventStore.Conversion
{
	using System;
	using System.Collections.Generic;
	using Logging;

	public class EventUpconverterPipelineHook : IPipelineHook
	{
		private static readonly ILog Logger = LogFactory.BuildLogger(typeof(EventUpconverterPipelineHook));
		private readonly IDictionary<Type, Func<object, object>> converters;

		public EventUpconverterPipelineHook(IDictionary<Type, Func<object, object>> converters)
		{
			if (converters == null)
				throw new ArgumentNullException("converters");

			this.converters = converters;
		}
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			this.converters.Clear();
		}

		public virtual Commit Select(Commit committed)
		{
			foreach (var eventMessage in committed.Events)
				eventMessage.Body = this.Convert(eventMessage.Body);

			return committed;
		}
		private object Convert(object source)
		{
			Func<object, object> converter;
			if (!this.converters.TryGetValue(source.GetType(), out converter))
				return source;

			var target = converter(source);
			Logger.Debug(Resources.ConvertingEvent, source.GetType(), target.GetType());

			return this.Convert(target);
		}

		public virtual bool PreCommit(Commit attempt)
		{
			return true;
		}
		public virtual void PostCommit(Commit committed)
		{
		}
	}
}