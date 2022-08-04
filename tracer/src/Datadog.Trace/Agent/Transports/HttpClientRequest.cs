// <copyright file="HttpClientRequest.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETCOREAPP
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Datadog.Trace.Logging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Agent.Transports
{
    internal class HttpClientRequest : IApiRequest, IMultipartApiRequest
    {
        private const string Boundary = "faa0a896-8bc8-48f3-b46d-016f2b15a884";
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<HttpClientRequest>();

        private readonly HttpClient _client;
        private readonly HttpRequestMessage _request;

        public HttpClientRequest(HttpClient client, Uri endpoint)
        {
            _client = client;
            _request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        }

        public void AddHeader(string name, string value)
        {
            _request.Headers.Add(name, value);
        }

        public async Task<IApiResponse> PostAsync(ArraySegment<byte> bytes, string contentType)
        {
            // re-create HttpContent on every retry because some versions of HttpClient always dispose of it, so we can't reuse.
            using (var content = new ByteArrayContent(bytes.Array, bytes.Offset, bytes.Count))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                _request.Content = content;

                var response = await _client.SendAsync(_request).ConfigureAwait(false);

                return new HttpClientResponse(response);
            }
        }

        public async Task<IApiResponse> PostAsync(params MultipartFormItem[] items)
        {
            if (items is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(items));
            }

            Log.Debug<int>("Sending multipart form request with {Count} items.", items.Length);

            using var formDataContent = new MultipartFormDataContent(boundary: Boundary);
            _request.Content = formDataContent;

            foreach (var item in items)
            {
                // Adds a form data item
                if (item.ContentInBytes is { } arraySegment)
                {
                    var content = new ByteArrayContent(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
                    Log.Debug("Adding to Multipart Byte Array | Name: {Name} | FileName: {FileName} | ContentType: {ContentType}", item.Name, item.FileName, item.ContentType);
                    content.Headers.ContentType = new MediaTypeHeaderValue(item.ContentType);
                    formDataContent.Add(content, item.Name, item.FileName);
                }
                else if (item.ContentInStream is { } stream)
                {
                    var content = new StreamContent(stream);
                    Log.Debug("Adding to Multipart Stream | Name: {Name} | FileName: {FileName} | ContentType: {ContentType}", item.Name, item.FileName, item.ContentType);
                    content.Headers.ContentType = new MediaTypeHeaderValue(item.ContentType);
                    formDataContent.Add(content, item.Name, item.FileName);
                }
            }

            var response = await _client.SendAsync(_request).ConfigureAwait(false);
            return new HttpClientResponse(response);
        }
    }
}
#endif
