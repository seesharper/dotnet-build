#load "Command.csx"

public static class Docker
{
    public static async Task BuildAsync(string repository, string tag, string workingDirectory)
    {
        await Command.ExecuteAsync("docker", $@"build --rm -f ""Dockerfile"" -t {repository}:{tag} .", workingDirectory);
    }

    public static async Task PushAsync(string repository, string tag, string workingDirectory)
    {
        var username = Environment.GetEnvironmentVariable("DOCKERHUB_USERNAME");
        var password = Environment.GetEnvironmentVariable("DOCKERHUB_PASSWORD");

        await Command.ExecuteAsync("docker", $"login --username {username} --password {password}", workingDirectory);
        await Command.ExecuteAsync("docker", $@"push {repository}:{tag}", workingDirectory);
    }
}