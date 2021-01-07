using System.Threading.Tasks;

namespace SignalFx.Tracing.Agent
{
    internal interface IApi
    {
        Task SendTracesAsync(Span[][] traces);
    }
}
