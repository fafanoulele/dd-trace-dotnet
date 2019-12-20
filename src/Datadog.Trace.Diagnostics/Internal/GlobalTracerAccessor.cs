using OpenTracing.Util;

namespace Datadog.Trace.Diagnostics.Internal
{
    public class GlobalTracerAccessor : IGlobalTracerAccessor
    {
        public ITracer GetGlobalTracer()
        {
            return GlobalTracer.Instance;
        }
    }
}
