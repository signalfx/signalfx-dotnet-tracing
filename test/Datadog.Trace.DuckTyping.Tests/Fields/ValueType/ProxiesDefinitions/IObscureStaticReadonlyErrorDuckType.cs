namespace SignalFx.Tracing.DuckTyping.Tests.Fields.ValueType.ProxiesDefinitions
{
    public interface IObscureStaticReadonlyErrorDuckType
    {
        [Duck(Name = "_publicStaticReadonlyValueTypeField", Kind = DuckKind.Field)]
        int PublicStaticReadonlyValueTypeField { get; set; }
    }
}
