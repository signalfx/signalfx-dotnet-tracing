// <copyright file="RequestPipeline_CallElasticsearch_Integration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Elasticsearch.V5
{
    /// <summary>
    /// Elasticsearch.Net.RequestPipeline.CallElasticsearch&lt;T&gt; calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = ElasticsearchV5Constants.ElasticsearchAssemblyName,
        TypeName = ElasticsearchV5Constants.RequestPipelineTypeName,
        MethodName = "CallElasticsearch",
        ReturnTypeName = "Elasticsearch.Net.ElasticsearchResponse`1<T>",
        ParameterTypeNames = new[] { "Elasticsearch.Net.RequestData" },
        MinimumVersion = ElasticsearchV5Constants.Version5,
        MaximumVersion = ElasticsearchV5Constants.Version5,
        IntegrationName = ElasticsearchV5Constants.IntegrationName)]
    // ReSharper disable once InconsistentNaming
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RequestPipeline_CallElasticsearch_Integration
    {
        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TRequestData">Type of the request</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="requestData">The request data</param>
        /// <returns>Calltarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget, TRequestData>(TTarget instance, TRequestData requestData)
        {
            var requestDataV5 = new RequestDataV5(requestData);
            var scope = ElasticsearchNetCommon.CreateScope(Tracer.Instance, ElasticsearchV5Constants.IntegrationId, instance.DuckCast<RequestPipelineStruct>(), requestDataV5);

            return new CallTargetState(scope, requestDataV5);
        }

        /// <summary>
        /// OnMethodEnd callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TResponse">Type of the response</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
        /// <param name="response">Response instance</param>
        /// <param name="exception">Exception instance in case the original code threw an exception.</param>
        /// <param name="state">Calltarget state value</param>
        /// <returns>A response value, in an async scenario will be T of Task of T</returns>
        internal static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception exception, in CallTargetState state)
        {
            if (state.State is IRequestData requestData)
            {
                state.Scope.SetDbStatementFromRequestData(requestData);
            }

            state.Scope.DisposeWithException(exception);
            return new CallTargetReturn<TResponse>(response);
        }
    }
}
