#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#load "nuget:ScriptUnit, 0.1.1"
#load "../Dotnet.Build/ReleaseNotes.csx"
#load "../Dotnet.Build/Git.csx"
#load "TestUtils.csx"

using static ScriptUnit;

await AddTestsFrom<ReleaseNotesTests>().Execute();

public class ReleaseNotesTests
{
    public void ShouldGenerateReleaseNotes()
    {
        using (var repoFolder = new DisposableFolder())
        {
            var repo = Git.Init(repoFolder.Path);
            repo.Execute("commit --allow-empty -m \"First Commit\"");
            repo.Execute("tag 1.0.0");
            ReleaseNotes.Generate(repoFolder.Path, Path.Combine(repoFolder.Path,"ReleaseNotes.md"));
        }
    }
}