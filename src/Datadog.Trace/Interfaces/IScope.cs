// TODO lucas
#pragma warning disable 1591

using System;

namespace Datadog.Trace.Interfaces
{
    public interface IScope : IDisposable
    {
        ISpan Span { get; }
    }
}
