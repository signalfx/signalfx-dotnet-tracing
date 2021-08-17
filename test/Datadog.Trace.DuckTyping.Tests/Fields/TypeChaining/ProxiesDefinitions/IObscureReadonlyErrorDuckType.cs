namespace SignalFx.Tracing.DuckTyping.Tests.Fields.TypeChaining.ProxiesDefinitions
{
    public interface IObscureReadonlyErrorDuckType
    {
        [Duck(Name = "_publicReadonlySelfTypeField", Kind = DuckKind.Field)]
        IDummyFieldObject PublicReadonlySelfTypeField { get; set; }
    }
}
