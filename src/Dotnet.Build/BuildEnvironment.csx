public static class BuildEnvironment
{
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
    
}