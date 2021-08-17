namespace SignalFx.Tracing.DuckTyping.Tests.Methods.ProxiesDefinitions
{
    public interface IDummyFieldObject
    {
        [Duck(Kind = DuckKind.Field)]
        int MagicNumber { get; set; }
    }
}
