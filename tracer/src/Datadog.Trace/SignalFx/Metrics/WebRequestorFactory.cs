using System;
using System.Collections.Generic;

namespace Datadog.Trace.SignalFx.Metrics
{
    internal class WebRequestorFactory
    {
        private Uri _uri;
        private string _method;
        private string _contentType;
        private int _timeout;
        private List<KeyValuePair<string, string>> _headers;

        public WebRequestorFactory()
        {
            _headers = new List<KeyValuePair<string, string>>();
        }

        public WebRequestorFactory WithUri(Uri uri)
        {
            _uri = uri;
            return this;
        }

        public WebRequestorFactory WithMethod(string method)
        {
            _method = method;
            return this;
        }

        public WebRequestorFactory WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        public WebRequestorFactory WithHeader(string header, string headerValue)
        {
            _headers.Add(new KeyValuePair<string, string>(header, headerValue));
            return this;
        }

        public WebRequestorFactory WithTimeout(int timeoutInMilliseconds)
        {
            _timeout = timeoutInMilliseconds;
            return this;
        }

        public WebRequestor GetRequestor()
        {
            var req = new WebRequestor(_uri)
                     .WithMethod(_method)
                     .WithContentType(_contentType)
                     .WithTimeout(_timeout);

            foreach (var header in _headers)
            {
                req.WithHeader(header.Key, header.Value);
            }

            return req;
        }
    }
}
