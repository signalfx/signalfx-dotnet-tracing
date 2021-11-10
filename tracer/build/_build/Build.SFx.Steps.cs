using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static CustomDotNetTasks;

// #pragma warning disable SA1306
// #pragma warning disable SA1134
// #pragma warning disable SA1111
// #pragma warning disable SA1400
// #pragma warning disable SA1401

partial class Build
{    
    AbsolutePath SnapshotsDirectory => TestsDirectory / "snapshots";

    Target OverwriteSnaphotFiles => _ => _
        .Description("Overwrites the *.verified.txt files with *.received.txt files used by VerifyTests")
        .Executes(() =>
        {
            foreach (var received in SnapshotsDirectory.GlobFiles("*.received.txt"))
            {
                var verified = received.ToString().Replace(".received.txt", ".verified.txt");
                File.Copy(received, verified, true);
            }
        });
}