# Polling Client

This project contains a 'reference' implementation of a Polling Client for NEventStore. 

Its duty is to constantly ask the NEventStore if there are new commits made by the users, read them and dispatch the committed events to their handlers.

You are encouraged to write your own PollingClient that suits your needs, you can use the code in this project as a starting point.

## New Polling client without RX	

To remove dependency from RX library, the old PollingClient was removed from the project. 

The entire code for OldPollingClient is included in this README, If you still need it in your project you can simply include this class in your code as well as the dependencies from RX.

With the new PollingClient2 you should simply pass a Function that will be called to handle the commit, you can use Rx if you like that approach approach, or even better you can use TPL Dataflow that is probably more suited.

If you want to use the new polling client with RX you can look at PollingClientRx inside Test project.

### Old Polling Client Code
	
	namespace NEventStore.Client
	{
	    using System;
	    using System.Collections.Generic;
	    using System.Reactive;
	    using System.Reactive.Subjects;
	    using System.Threading;
	    using System.Threading.Tasks;
	    using NEventStore.Logging;
	    using NEventStore.Persistence;
	
	    /// <summary>
	    /// Represents a client that poll the storage for latest commits.
	    /// </summary>
	    public sealed class PollingClient : ClientBase
	    {
	        private readonly int _interval;
	
	        public PollingClient(IPersistStreams persistStreams, int interval = 5000) : base(persistStreams)
	        {
	            if (persistStreams == null)
	            {
	                throw new ArgumentNullException("persistStreams");
	            }
	            if (interval <= 0)
	            {
	                throw new ArgumentException(Messages.MustBeGreaterThanZero.FormatWith("interval"));
	            }
	            _interval = interval;
	        }
	
	        /// <summary>
	        /// Observe commits from the sepecified checkpoint token. If the token is null,
	        /// all commits from the beginning will be observed.
	        /// </summary>
	        /// <param name="checkpointToken">The checkpoint token.</param>
	        /// <returns>
	        /// An <see cref="IObserveCommits" /> instance.
	        /// </returns>
	        public override IObserveCommits ObserveFrom(Int64 checkpointToken = 0)
	        {
	            return new PollingObserveCommits(PersistStreams, _interval, null, checkpointToken);
	        }
	
	        public override IObserveCommits ObserveFromBucket(string bucketId, Int64 checkpointToken = 0)
	        {
	            return new PollingObserveCommits(PersistStreams, _interval, bucketId, checkpointToken);
	        }
	
	        private class PollingObserveCommits : IObserveCommits
	        {
	            private ILog Logger = LogFactory.BuildLogger(typeof (PollingClient));
	            private readonly IPersistStreams _persistStreams;
	            private Int64 _checkpointToken;
	            private readonly int _interval;
	            private readonly string _bucketId;
	            private readonly Subject<ICommit> _subject = new Subject<ICommit>();
	            private readonly CancellationTokenSource _stopRequested = new CancellationTokenSource();
	            private TaskCompletionSource<Unit> _runningTaskCompletionSource;
	            private int _isPolling = 0;
	
	            public PollingObserveCommits(IPersistStreams persistStreams, int interval, string bucketId, Int64 checkpointToken = 0)
	            {
	                _persistStreams = persistStreams;
	                _checkpointToken = checkpointToken;
	                _interval = interval;
	                _bucketId = bucketId;
	            }
	
	            public IDisposable Subscribe(IObserver<ICommit> observer)
	            {
	                return _subject.Subscribe(observer);
	            }
	
	            public void Dispose()
	            {
	                _stopRequested.Cancel();
	                _subject.Dispose();
	                if (_runningTaskCompletionSource != null)
	                {
	                    _runningTaskCompletionSource.TrySetResult(new Unit());
	                }
	            }
	
	            public Task Start()
	            {
	                if (_runningTaskCompletionSource != null)
	                {
	                    return _runningTaskCompletionSource.Task;
	                }
	                _runningTaskCompletionSource = new TaskCompletionSource<Unit>();
	                PollLoop();
	                return _runningTaskCompletionSource.Task;
	            }
	
	            public void PollNow()
	            {
	                DoPoll();
	            }
	
	            private void PollLoop()
	            {
	                if (_stopRequested.IsCancellationRequested)
	                {
	                    Dispose();
	                    return;
	                }
	                TaskHelpers.Delay(_interval, _stopRequested.Token)
	                    .WhenCompleted(_ =>
	                    {
	                        DoPoll();
	                        PollLoop();
	                    },_ => Dispose());
	            }
	
	            private void DoPoll()
	            {
	                if (Interlocked.CompareExchange(ref _isPolling, 1, 0) == 0)
	                {
	                    try
	                    {
	                        var commits = _bucketId == null ? 
	                            _persistStreams.GetFrom(_checkpointToken) :
	                            _persistStreams.GetFrom(_bucketId, _checkpointToken);
	
	                        foreach (var commit in commits)
	                        {
	                            if (_stopRequested.IsCancellationRequested)
	                            {
	                                _subject.OnCompleted();
	                                return;
	                            }
	                            _subject.OnNext(commit);
	                            _checkpointToken = commit.CheckpointToken;
	                        }
	                    }
	                    catch (Exception ex)
	                    {
	                        // These exceptions are expected to be transient
	                        Logger.Error(ex.ToString());
	                    }
	                    Interlocked.Exchange(ref _isPolling, 0);
	                }
	            }
	        }
	    }
	
	    public abstract class ClientBase
	    {
	        private readonly IPersistStreams _persistStreams;
	
	        protected ClientBase(IPersistStreams persistStreams)
	        {
	            _persistStreams = persistStreams;
	        }
	
	        protected IPersistStreams PersistStreams
	        {
	            get { return _persistStreams; }
	        }
	
	
	        /// <summary>
	        /// Observe commits from the sepecified checkpoint token. If the token is null,
	        ///  all commits from the beginning will be observed.
	        /// </summary>
	        /// <param name="checkpointToken">The checkpoint token.</param>
	        /// <returns>An <see cref="IObserveCommits"/> instance.</returns>
	        public abstract IObserveCommits ObserveFrom(Int64 checkpointToken = 0);
	
	        /// <summary>
	        /// Observe commits from a bucket after the sepecified checkpoint token. If the token is null,
	        ///  all commits from the beginning will be observed.
	        /// </summary>
	        /// <param name="bucketId">The bucket id</param>
	        /// <param name="checkpointToken">The checkpoint token.</param>
	        /// <returns>An <see cref="IObserveCommits"/> instance.</returns>
	        public abstract IObserveCommits ObserveFromBucket(string bucketId, Int64 checkpointToken = 0);
	    }
	
	    public interface IObserveCommits : IObservable<ICommit>, IDisposable
	    {
	        Task Start();
	
	        void PollNow();
	    }
	}

