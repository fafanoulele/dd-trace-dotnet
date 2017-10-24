﻿using MsgPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Datadog.Tracer.IntegrationTests
{
    /// <summary>
    /// This class implements a handler that can be passed as parameter of a new HttpClient
    /// and will record all requests made by that client.
    /// </summary>
    /// <seealso cref="System.Net.Http.DelegatingHandler" />
    public class RecordHttpHandler : DelegatingHandler
    {
        private Object _lock = new Object();
        private int _count = 0;
        private int _target = 0;
        private TaskCompletionSource<bool> _tcs;

        public List<Tuple<HttpRequestMessage, byte[]>> Requests { get; set; }

        public List<IList<MessagePackObject>> Traces => Requests
            .Where(x => x.Item1.RequestUri.ToString().Contains("/v0.3/traces"))
            .Select(x => Unpacking.UnpackObject(x.Item2).Value.AsList())
            .ToList();

        public List<MessagePackObjectDictionary> Services => Requests
            .Where(x => x.Item1.RequestUri.ToString().Contains("/v0.3/services"))
            .Select(x => Unpacking.UnpackObject(x.Item2).Value.AsDictionary())
            .ToList();

        public List<HttpResponseMessage> Responses { get; set;  }

        public RecordHttpHandler()
        {
            InnerHandler = new HttpClientHandler();
            Requests = new List<Tuple<HttpRequestMessage, byte[]>>();
            Responses = new List<HttpResponseMessage>();
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestContent = await request.Content.ReadAsByteArrayAsync();
            var response =  await base.SendAsync(request, cancellationToken);
            lock(_lock)
            {
                Requests.Add(Tuple.Create(request, requestContent));
                Responses.Add(response);
                _count++;
                if(_tcs != null && _count >= _target)
                {
                    _tcs.SetResult(true);
                    _tcs = null;
                }
            }
            return response;
        }

        public Task<bool> WaitForCompletion(int target, TimeSpan? timeout = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(10);
            lock (_lock)
            {
                if (_count >= target)
                {
                    return Task.FromResult(true);
                }
                if (_tcs == null)
                {
                    _target = target;
                    _tcs = new TaskCompletionSource<bool>();
                    var cancelationSource = new CancellationTokenSource(timeout.Value);
                    cancelationSource.Token.Register(() => _tcs?.SetException(new TimeoutException()));
                    return _tcs.Task;
                }
                else
                {
                    throw new InvalidOperationException("This method should not be called twice on the same instance");
                }
            }
        }
    }
}
