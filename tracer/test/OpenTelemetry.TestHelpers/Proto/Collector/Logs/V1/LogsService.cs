// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: logs_service.proto
// </auto-generated>

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace OpenTelemetry.TestHelpers.Proto.Collector.Logs.V1
{

    [global::ProtoBuf.ProtoContract()]
    public partial class ExportLogsServiceRequest : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::ProtoBuf.ProtoMember(1, Name = @"resource_logs")]
        public global::System.Collections.Generic.List<global::OpenTelemetry.TestHelpers.Proto.Logs.V1.ResourceLogs> ResourceLogs { get; } = new global::System.Collections.Generic.List<global::OpenTelemetry.TestHelpers.Proto.Logs.V1.ResourceLogs>();

    }

    [global::ProtoBuf.ProtoContract()]
    public partial class ExportLogsServiceResponse : global::ProtoBuf.IExtensible
    {
        private global::ProtoBuf.IExtension __pbn__extensionData;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion
