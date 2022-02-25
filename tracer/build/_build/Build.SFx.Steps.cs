using System.IO;
using Nuke.Common;
using Nuke.Common.IO;

partial class Build
{    
    AbsolutePath SnapshotsDirectory => TestsDirectory / "snapshots";

    Target OverwriteSnapshotFiles => _ => _
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
