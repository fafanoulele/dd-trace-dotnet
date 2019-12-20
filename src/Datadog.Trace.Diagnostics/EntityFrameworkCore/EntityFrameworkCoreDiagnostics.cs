using System;
using Datadog.Trace.Diagnostics.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing;

namespace Datadog.Trace.Diagnostics.EntityFrameworkCore
{
    internal sealed class EntityFrameworkCoreDiagnostics : DiagnosticListenerObserver
    {
        // https://github.com/aspnet/EntityFrameworkCore/blob/dev/src/EFCore/DbLoggerCategory.cs
        public const string DiagnosticListenerName = "Microsoft.EntityFrameworkCore";

        private const string TagMethod = "db.method";
        private const string TagIsAsync = "db.async";

        private readonly EntityFrameworkCoreDiagnosticOptions _options;

        protected override string GetListenerName() => DiagnosticListenerName;

        public EntityFrameworkCoreDiagnostics(ILoggerFactory loggerFactory, ITracer tracer,
            IOptions<EntityFrameworkCoreDiagnosticOptions> options, IOptions<GenericEventOptions> genericEventOptions)
            : base(loggerFactory, tracer, genericEventOptions.Value)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting":
                    {
                        CommandEventData args = (CommandEventData)untypedArg;

                        string operationName = _options.OperationNameResolver(args);

                        Tracer.BuildSpan(operationName)
                            .WithTag(OpenTracing.Tag.Tags.SpanKind, OpenTracing.Tag.Tags.SpanKindClient)
                            .WithTag(OpenTracing.Tag.Tags.Component, _options.ComponentName)
                            .WithTag(OpenTracing.Tag.Tags.DbInstance, args.Command.Connection.Database)
                            .WithTag(OpenTracing.Tag.Tags.DbStatement, args.Command.CommandText)
                            .WithTag(TagMethod, args.ExecuteMethod.ToString())
                            .WithTag(TagIsAsync, args.IsAsync)
                            .StartActive();
                    }
                    break;

                case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted":
                    {
                        DisposeActiveScope(isScopeRequired: true);
                    }
                    break;

                case "Microsoft.EntityFrameworkCore.Database.Command.CommandError":
                    {
                        CommandErrorEventData args = (CommandErrorEventData)untypedArg;

                        // The "CommandExecuted" event is NOT called in case of an exception,
                        // so we have to dispose the scope here as well!
                        DisposeActiveScope(isScopeRequired: true, exception: args.Exception);
                    }
                    break;

                default:
                    {
                        ProcessUnhandledEvent(eventName, untypedArg);
                    }
                    break;
            }
        }
    }
}
