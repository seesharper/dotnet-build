#! "netcoreapp2.0"
#r "nuget: FluentAssertions, 4.19.4"
#r "nuget:Octokit, 0.27.0"
#load "../Dotnet.Build/FileUtils.csx"
#load "../Dotnet.Build/GitHub-ReleaseManager.csx"
#load "nuget:ScriptUnit, 0.1.3"
#load "TestUtils.csx"

using Octokit;
using FluentAssertions;
using static ReleaseManagement;
using static FileUtils;
using static ScriptUnit;

// await AddTestsFrom<ReleaseManagerTests>().AddFilter(m => m.IsDefined(typeof(OnlyThisAttribute), true)).Execute();
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