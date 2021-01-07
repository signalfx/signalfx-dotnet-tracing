using System.Threading.Tasks;

namespace SignalFx.Tracing.Agent
{
    internal interface IAgentWriter
    {
        void WriteTrace(Span[] trace);

        Task FlushAndCloseAsync();

        void OverrideApi(IApi api);
    }
}
