using System;
using System.IO;
using System.Net;

namespace Datadog.Trace.SignalFx.Metrics
{
    internal class WebRequestor
    {
        private readonly HttpWebRequest _request;

        public WebRequestor(Uri uri)
        {
            _request = WebRequest.CreateHttp(uri);
        }

        public WebRequestor WithMethod(string method)
        {
            _request.Method = method;
            return this;
        }

        public WebRequestor WithContentType(string contentType)
        {
            _request.ContentType = contentType;
            return this;
        }

        public WebRequestor WithHeader(string name, string value)
        {
            _request.Headers.Add(name, value);
            return this;
        }

        public Stream GetWriteStream()
        {
            return _request.GetRequestStream();
        }

        public WebResponse Send()
        {
            return _request.GetResponse();
        }

        public WebRequestor WithTimeout(int timeoutInMilliseconds)
        {
            _request.Timeout = timeoutInMilliseconds;
            return this;
        }
    }
}
