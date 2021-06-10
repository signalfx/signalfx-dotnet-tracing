using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using SignalFx.Tracing;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Headers;

namespace ContextPropagation
{
    [CategoriesColumn]
    [MemoryDiagnoser]
    public class B3
    {
        private static readonly Dictionary<string, string> s_commonHeaders = new Dictionary<string, string>
        {
            { "Connection", "Keep-Alive" },
            { "Content-Length", "4" },
            { "Content-Type", "text/plain; charset=utf-8" },
            { "Host", "localhost:9000" },
        };
        private static readonly DictionaryHeadersCollection s_noB3Header = new DictionaryHeadersCollection();
        private static readonly DictionaryHeadersCollection s_B3Header = new DictionaryHeadersCollection();

        private static readonly Tracer s_tracer;

        static B3()
        {
            s_tracer = new Tracer(new TracerSettings());

            foreach (var entry in s_commonHeaders)
            {
                s_noB3Header.Add(entry.Key, entry.Value);
                s_B3Header.Add(entry.Key, entry.Value);
            }

            s_B3Header.Add("x-b3-traceid", TraceId.CreateFromInt(123).ToString());
            s_B3Header.Add("x-b3-spanid", (123ul).ToString("x16"));
        }

        [Benchmark]
        public void NoB3()
        {
            var spanCtx = s_tracer.Propagator.Extract(s_noB3Header);
            if (spanCtx != null)
            {
                // Avoiding this to be compiled away.
                throw new ApplicationException("Found a span context where it was not expected.");
            }
        }

        [Benchmark]
        public void B3Header()
        {
            var spanCtx = s_tracer.Propagator.Extract(s_B3Header);
            if (spanCtx == null)
            {
                // Avoiding this to be compiled away.
                throw new ApplicationException("Null span context where it was not expected.");
            }
        }
    }
}
