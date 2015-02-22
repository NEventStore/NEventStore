namespace CommonDomain
{
	using System;

	using CommonDomain.Core;

	public class TestSaga : SagaBase<TestSagaMessage>
	{
		public TestSaga(string id)
		{
			Id = id;
		}
	}

	public abstract class TestSagaMessage { }
}