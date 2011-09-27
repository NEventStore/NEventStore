using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventStore
{
    interface IConvertEvents<in TSource, out TTarget>
    {
        TTarget Convert(TSource sourceEvent);
    }
}
