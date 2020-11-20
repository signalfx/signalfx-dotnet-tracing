// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Datadog.Trace.TestHelpers.HttpMessageHandlers
{
    /// <summary>
    /// This class implements a handler that can be passed as parameter of a new HttpClient
    /// and will record all requests made by that client.
    /// </summary>
    /// <seealso cref="System.Net.Http.DelegatingHandler" />
    public class RecordHttpHandler : DelegatingHandler
    {
        private object _lock = new object();
        private int _count = 0;
        private int _target = 0;
        private TaskCompletionSource<bool> _tcs;

        public RecordHttpHandler()
        {
            InnerHandler = new HttpClientHandler();
            Requests = new List<Tuple<HttpRequestMessage, byte[]>>();
            Responses = new List<HttpResponseMessage>();
        }

        public List<Tuple<HttpRequestMessage, byte[]>> Requests { get; set; }

        public List<JToken> ZipkinTraces => Requests
            .Where(x => x.Item1.RequestUri.ToString().Contains("/v1/trace"))
            .Select(x =>
                {
                    string item;
                    using (StreamReader reader = new StreamReader(new MemoryStream(x.Item2), Encoding.UTF8))
                    {
                        item = reader.ReadToEnd();
                    }
                    JToken parsed = JToken.Parse(item);
                    return parsed;
                }).ToList();

        public List<HttpResponseMessage> Responses { get; set; }

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
                    var cancellationSource = new CancellationTokenSource(timeout.Value);
                    cancellationSource.Token.Register(() => _tcs?.SetException(new TimeoutException()));
                    return _tcs.Task;
                }
                else
                {
                    throw new InvalidOperationException("This method should not be called twice on the same instance");
                }
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestContent = await request.Content.ReadAsByteArrayAsync();
            var response = await base.SendAsync(request, cancellationToken);
            lock (_lock)
            {
                Requests.Add(Tuple.Create(request, requestContent));
                Responses.Add(response);
                _count++;
                if (_tcs != null && _count >= _target)
                {
                    _tcs.SetResult(true);
                    _tcs = null;
                }
            }

            return response;
        }
    }
}
