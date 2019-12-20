using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.Sampling;

namespace Datadog.Trace
{
    internal interface IDatadogTracer
    {
        string DefaultServiceName { get; }

        bool IsDebugEnabled { get; }

        IScopeManager ScopeManager { get; }

        ISampler Sampler { get; }

        TracerSettings Settings { get; }

        Span StartSpan(string operationName);

        Span StartSpan(string operationName, ISpanContext parent);

        Span StartSpan(string operationName, ISpanContext parent, string serviceName, DateTimeOffset? startTime, bool ignoreActiveScope);

        void Write(List<Span> span);

        /// <summary>
        /// Make a span active and return a scope that can be disposed to close the span
        /// </summary>
        /// <param name="span">The span to activate</param>
        /// <param name="finishOnClose">If set to false, closing the returned scope will not close the enclosed span </param>
        /// <returns>A Scope object wrapping this span</returns>
        Scope ActivateSpan(Span span, bool finishOnClose = true);
    }
}
