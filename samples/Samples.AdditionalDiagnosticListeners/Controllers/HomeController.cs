// Modified by SignalFx
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Samples.AdditionalDiagnosticListeners.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var instrumentationType = Type.GetType("Datadog.Trace.ClrProfiler.Instrumentation, Datadog.Trace.ClrProfiler.Managed");
            ViewBag.ProfilerAttached = instrumentationType?.GetProperty("ProfilerAttached", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ?? false;
            ViewBag.TracerAssemblyLocation = Type.GetType("Datadog.Trace.Tracer, Datadog.Trace")?.Assembly.Location;
            ViewBag.ClrProfilerAssemblyLocation = instrumentationType?.Assembly.Location;

            var prefixes = new[] { "COR_", "CORECLR_", "DD_", "DATADOG_", "SIGNALFX_" };

            var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                          from prefix in prefixes
                          let key = (envVar.Key as string)?.ToUpperInvariant()
                          let value = envVar.Value as string
                          where key.StartsWith(prefix)
                          orderby key
                          select new KeyValuePair<string, string>(key, value);

            return View(envVars.ToList());
        }

        [Route("alive-check")]
        public string IsAlive()
        {
            return "Yes";
        }
    }
}
