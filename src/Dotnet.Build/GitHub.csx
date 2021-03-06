#load "nuget:github-changelog, 0.1.5"
#load "GitHub-ReleaseManager.csx"
#load "Logger.csx"
#load "Git.csx"
#load "BuildEnvironment.csx"
#load "BuildContext.csx"

using static ChangeLog;
using static ReleaseManagement;
public static class GitHub
{
    public async static Task CreateChangeLog()
    {
        Logger.Log("Creating release notes");
        var generator = ChangeLogFrom(BuildContext.Owner, BuildContext.ProjectName, BuildEnvironment.GitHubAccessToken).SinceLatestTag();
        if (!Git.Default.IsTagCommit())
        {
            generator = generator.IncludeUnreleased();
        }
        await generator.Generate(BuildContext.GitHubReleaseNotesPath, FormattingOptions.Default.WithPullRequestBody());
    }

    public async static Task Release()
    {
        var assetsFiles = Directory.GetFiles(BuildContext.GitHubArtifactsFolder).Where(f => !f.Equals(BuildContext.GitHubReleaseNotesPath, StringComparison.InvariantCultureIgnoreCase)).ToArray();
        var octetStreamAssets = assetsFiles.Select(f => new OctetStreamReleaseAsset(f)).ToArray();

        await ReleaseManagerFor(BuildContext.Owner, BuildContext.ProjectName, BuildEnvironment.GitHubAccessToken)
       .CreateRelease(BuildContext.LatestTag, BuildContext.GitHubReleaseNotesPath, octetStreamAssets);
    }
}