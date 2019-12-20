using System;
using System.Data.SqlClient;
using Datadog.Trace.Diagnostics.CoreFx;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Datadog.Trace.Diagnostics.Internal;
using OpenTracing.Tag;

namespace Datadog.Trace.Diagnostics.CoreFx
{
    internal sealed class SqlClientDiagnostics : DiagnosticListenerObserver
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        private static readonly PropertyFetcher _activityCommand_RequestFetcher = new PropertyFetcher("Command");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly SqlClientDiagnosticOptions _options;

        public SqlClientDiagnostics(ILoggerFactory loggerFactory, IDatadogTracer tracer, IOptions<SqlClientDiagnosticOptions> options,
            IOptions<GenericEventOptions> genericEventOptions)
           : base(loggerFactory, tracer, genericEventOptions.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override string GetListenerName() => DiagnosticListenerName;

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "System.Data.SqlClient.WriteCommandBefore":
                    {
                        var args = (SqlCommand)_activityCommand_RequestFetcher.Fetch(untypedArg);

                        string operationName = _options.OperationNameResolver(args);

                        Tracer.BuildSpan(operationName)
                            .WithTag(Tags.SpanKind, Tags.SpanKindClient)
                            .WithTag(Tags.Component, _options.ComponentName)
                            .WithTag(Tags.DbInstance, args.Connection.Database)
                            .WithTag(Tags.DbStatement, args.CommandText)
                            .StartActive();
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandError":
                    {
                        Exception ex = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);

                        DisposeActiveScope(isScopeRequired: true, exception: ex);
                    }
                    break;

                case "System.Data.SqlClient.WriteCommandAfter":
                    {
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;
            }
        }
    }
}
