#r "nuget: AwesomeAssertions, 9.3.0"
#r "nuget:Octokit, 14.0.0"
#load "../Dotnet.Build/FileUtils.csx"
#load "../Dotnet.Build/GitHub.csx"
#load "../Dotnet.Build/GitHub-ReleaseManager.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using Octokit;
using AwesomeAssertions;
using static ReleaseManagement;
using static FileUtils;
using static ScriptUnit;
using DisposableFolder = FileUtils.DisposableFolder;
//await AddTestsFrom<ReleaseManagerTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
//await AddTestsFrom<ReleaseManagerTests>().Execute();

public class ReleaseManagerTests
{
    private static string Owner = "seesharper";
    private static string Repository = "release-fixture";


    public async Task ShouldCreateRelease()
    {
        var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");
        var client = CreateClient();
        await DeleteAllReleases(client);

        using (var disposableFolder = new DisposableFolder())
        {
            var pathToReleaseNotes = Path.Combine(disposableFolder.Path, "ReleaseNotes.md");
            File.WriteAllText(pathToReleaseNotes, "This is some release notes");
            await ReleaseManagerFor(Owner, Repository, accessToken).CreateRelease("0.1.0", pathToReleaseNotes, new[] { new ZipReleaseAsset(pathToReleaseNotes) });
        }

        var latestRelease = await client.Repository.Release.GetLatest(Owner, Repository);
        latestRelease.Name.Should().Be("0.1.0");
        latestRelease.Body.Should().Be("This is some release notes");
    }

    [OnlyThis]
    public async Task ShouldUpdateRelease()
    {
        var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");
        var client = CreateClient();
        await DeleteAllReleases(client);

        var newRelease = new NewRelease("0.1.0");
        newRelease.Name = "Release Name";
        newRelease.Body = "Release Body";
        await client.Repository.Release.Create(Owner, Repository, newRelease);

        using (var disposableFolder = new DisposableFolder())
        {
            var pathToReleaseNotes = Path.Combine(disposableFolder.Path, "ReleaseNotes.md");
            File.WriteAllText(pathToReleaseNotes, "This is some release notes");
            await ReleaseManagerFor(Owner, Repository, accessToken).CreateRelease("0.1.0", pathToReleaseNotes, new[] { new ZipReleaseAsset(pathToReleaseNotes) });
        }

        var latestRelease = await client.Repository.Release.GetLatest(Owner, Repository);
        latestRelease.Name.Should().Be("0.1.0");
        latestRelease.Body.Should().Be("This is some release notes");
    }


    public async Task ShouldUploadGitHubAssets()
    {
        var client = CreateClient();
        await DeleteAllReleases(client);

        using (var solutionFolder = new DisposableFolder())
        {
            Command.Execute("git", "clone https://github.com/seesharper/release-fixture.git", solutionFolder.Path);
            var oldRepoFolder = BuildContext.RepositoryFolder;
            BuildContext.RepositoryFolder = Path.Combine(solutionFolder.Path, "release-fixture");



            var pathToReleaseNotes = Path.Combine(BuildContext.GitHubArtifactsFolder, "ReleaseNotes.md");
            File.WriteAllText(pathToReleaseNotes, "This is some release notes");

            var pathToSomeBinaryFile = Path.Combine(BuildContext.GitHubArtifactsFolder, "SomeBinaryFile");
            File.WriteAllBytes(pathToSomeBinaryFile, new byte[] { 42 });

            await GitHub.Release();

            var latestRelease = await client.Repository.Release.GetLatest(Owner, Repository);
            latestRelease.Assets.Should().Contain(a => a.Name == "SomeBinaryFile");

            BuildContext.RepositoryFolder = oldRepoFolder;
            //await ReleaseManagerFor(Owner, Repository, accessToken).CreateRelease("0.1.0", pathToReleaseNotes, new[] { new ZipReleaseAsset(pathToReleaseNotes) });
        }
    }



    private static GitHubClient CreateClient()
    {
        var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");

        var client = new GitHubClient(new ProductHeaderValue("release-fixture"));
        var tokenAuth = new Credentials(accessToken);
        client.Credentials = tokenAuth;
        return client;
    }

    private static async Task DeleteAllReleases(GitHubClient client)
    {
        var allReleases = await client.Repository.Release.GetAll(Owner, Repository);
        foreach (var release in allReleases)
        {
            await client.Repository.Release.Delete(Owner, Repository, release.Id);
        }
    }



}