#r "nuget: Newtonsoft.Json, 13.0.1"

using Newtonsoft.Json;

public class DependencyReport
{
    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("parameters")]
    public string Parameters { get; set; } = string.Empty;

    [JsonProperty("sources")]
    public List<string> Sources { get; set; } = new List<string>();

    [JsonProperty("projects")]
    public List<Project> Projects { get; set; } = new List<Project>();

    public class Framework
    {
        [JsonProperty("framework")]
        public string TargetFramework { get; set; } = string.Empty;

        [JsonProperty("topLevelPackages")]
        public List<TopLevelPackage> TopLevelPackages { get; set; } = new List<TopLevelPackage>();
    }

    public class Project
    {
        [JsonProperty("path")]
        public string Path { get; set; } = string.Empty;

        [JsonProperty("frameworks")]
        public List<Framework> Frameworks { get; set; } = new List<Framework>();
    }

    public class TopLevelPackage
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("requestedVersion")]
        public string RequestedVersion { get; set; } = string.Empty;

        [JsonProperty("resolvedVersion")]
        public string ResolvedVersion { get; set; } = string.Empty;

        [JsonProperty("latestVersion")]
        public string LatestVersion { get; set; } = string.Empty;
    }
}