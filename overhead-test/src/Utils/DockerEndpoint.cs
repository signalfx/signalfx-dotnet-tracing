using System;

namespace SignalFx.OverheadTest.Utils;

internal class DockerEndpoint
{
    public DockerEndpoint(string hostname)
    {
        if (string.IsNullOrEmpty(hostname)) throw new ArgumentException("Invalid hostname", nameof(hostname));
        Hostname = hostname;
    }

    public string Url => $"tcp://{Hostname}:2375";

    public string Hostname { get; }
}
