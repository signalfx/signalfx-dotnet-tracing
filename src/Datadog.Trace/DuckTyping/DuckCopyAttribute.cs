using System;

namespace SignalFx.Tracing.DuckTyping
{
    /// <summary>
    /// Duck copy struct attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class DuckCopyAttribute : Attribute
    {
    }
}
