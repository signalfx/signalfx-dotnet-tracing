namespace SignalFx.Tracing.DuckTyping.Tests.Properties.ValueType.ProxiesDefinitions
{
    public interface IStructDuckType : IDuckType
    {
        int PublicGetSetValueType { get; }

        int PrivateGetSetValueType { get; }
    }
}
