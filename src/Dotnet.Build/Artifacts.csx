#load "BuildEnvironment.csx"
#load "BuildContext.csx"
#load "GitHub.csx"
#load "Git.csx"
#load "NuGet.csx"
public static class Artifacts
{
    public async static Task Deploy()
    {
        if (!BuildEnvironment.IsSecure)
        {
            Logger.Log("Publishing artifacts can only be done in a secure environment");
            return;
        }

        await GitHub.CreateChangeLog();

        if (!BuildContext.IsTagCommit)
        {
            Logger.Log("Publishing artifacts can only be done if we are on a tag commit");
            return;
        }

        Git.Open(BuildContext.RepositoryFolder).RequireCleanWorkingTree();

        await GitHub.Release();

        NuGet.TryPush();

    }
}