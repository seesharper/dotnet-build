#r "nuget:Octokit, 0.27.0"
#load "ReleaseNotes.csx"
#load "FileUtils.csx"
#load "Git.csx"
#load "FileUtils.csx"
#load "Logger.csx"

using Octokit;
using static FileUtils;

public static class GitHub
{
    public static void Pack(string pathToPublishFolder, string githubReleaseFolder)
    {
        ReleaseNotes.Generate(Path.Combine(githubReleaseFolder, "CHANGELOG.MD"));
        string latestTag = Git.Default.GetLatestTag();
        string projectName =Git.Default.GetRepositoryInfo().ProjectName;
        string zipFileName = $"{projectName}.{latestTag}.zip";
        string zipFilePath = Path.Combine(githubReleaseFolder, zipFileName);
        Zip(pathToPublishFolder, zipFilePath);
    }

    public static void CreateReleaseDraft(string githubReleaseFolder)
    {
        var repositoryInfo = Git.Default.GetRepositoryInfo();
        var accessToken = System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");
        var client = new GitHubClient(new ProductHeaderValue("dotnet-script"));
        string latestTag = Git.Default.GetLatestTag();

        var releaseNotes = ReadFile(Path.Combine(githubReleaseFolder,"ReleaseNotes.md"));

        var newRelease = new NewRelease(latestTag);    
        newRelease.Name = latestTag;
        newRelease.Body = releaseNotes;
        newRelease.Draft = true;        
        newRelease.Prerelease = latestTag.Contains("-");

        var tokenAuth = new Credentials(accessToken); 
        client.Credentials = tokenAuth;

        var createdRelease = client.Repository.Release.Create(repositoryInfo.Owner, repositoryInfo.ProjectName, newRelease).Result;

        var assets = Directory.GetFiles(githubReleaseFolder,"*.zip");
        foreach(var asset in assets)
        {
            var archiveContents = File.OpenRead(asset);
            var assetUpload = new ReleaseAssetUpload() 
            {
                FileName = Path.GetFileName(asset),
                ContentType = "application/zip",
                RawData = archiveContents
            };
            client.Repository.Release.UploadAsset(createdRelease, assetUpload).Wait();
        }
    }
}