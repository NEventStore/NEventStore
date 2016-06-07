using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NEventStore.Helpers
{
    internal sealed class DisposableAction : IDisposable
    {

        public static readonly DisposableAction Empty = new DisposableAction(null);

        private Action _disposeAction;

        public DisposableAction(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            // Interlocked allows the continuation to be executed only once
            Action dispose = Interlocked.Exchange(ref _disposeAction, null);
            if (dispose != null)
            {
                dispose();
            }
        }

    }
}
