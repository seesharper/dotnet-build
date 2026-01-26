#r "nuget:Microsoft.DotNet.PlatformAbstractions, 3.1.6"
#load "Git.csx"
using System.Runtime.InteropServices;

public static class BuildEnvironment
{
    private static Lazy<bool> _isTagCommit = new Lazy<bool>(() => Git.Default.IsTagCommit());

    private static Lazy<string> _currentTag = new Lazy<string>(() => Git.Default.GetLatestTag());

    /// <summary>
    /// Determines if we are in a "secure" environment with access to secure environment variables.
    /// </summary>
    /// <returns></returns>
    public static bool IsSecure => System.Environment.GetEnvironmentVariable("IS_SECURE_BUILDENVIRONMENT") == "true";

    /// <summary>
    /// Gets the GitHub access token used to authenticate with GitHub.
    /// </summary>
    /// <returns></returns>
    public static string GitHubAccessToken => System.Environment.GetEnvironmentVariable("GITHUB_REPO_TOKEN");

    /// <summary>
    /// Gets the NuGet API key used to push packages to NuGet.
    /// </summary>
    /// <returns></returns>
    public static string NuGetApiKey => System.Environment.GetEnvironmentVariable("NUGET_APIKEY");

    /// <summary>
    /// Gets the Chocolatey API key used to push packages to Chocolatey.
    /// </summary>
    /// <returns></returns>
    public static string ChocolateyApiKey = System.Environment.GetEnvironmentVariable("CHOCOLATEY_APIKEY");

    /// <summary>
    /// Gets a value that indicates if we are running on Windows.
    /// </summary>
    /// <returns>true if we are running on Windows, otherwise false.</returns>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets a value that indicates if we are running on Linux.
    /// </summary>
    /// <returns>true if we are running on Linux, otherwise false.</returns>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets a value that indicates if we are running on OSX.
    /// </summary>
    /// <returns>true if we are running on OSX, otherwise false.</returns>
    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary>
    /// Gets a value that indicates if we are on a tag commit.
    /// </summary>
    /// <returns>true if we are running on a tag commit, otherwise false.</returns>
    public static bool IsTagCommit => _isTagCommit.Value;

    /// <summary>
    /// Gets the latest tag fro this repository.
    /// </summary>
    public static bool LatestTag => _isTagCommit.Value;

    /// <summary>
    /// Ensures that we have a clean working tree (git)
    /// </summary>
    public static void RequireCleanWorkingTree()
    {
        Git.Default.RequireCleanWorkingTree();
    }
}